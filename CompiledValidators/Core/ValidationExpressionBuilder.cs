using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CompiledValidators.Core
{
    internal class ValidationExpressionBuilder
    {
        private static readonly MethodInfo Collection_Add = typeof(ICollection<ValidationError>).GetMethod("Add", new[] { typeof(ValidationError) });
        private static readonly ConstructorInfo ValidationError_Ctor = typeof(ValidationError).GetConstructor(new[] { typeof(int), typeof(object) });

        private readonly IRecursionPolicy _recursionPolicy;
        private readonly IValidatorProvider _validatorProvider;
        private readonly IEnumerable<IValidationExpressionConverter> _converters;

        private readonly ConcurrentDictionary<Type, TypeData> _typeData;
        private readonly ConcurrentDictionary<Type, EnumerationData> _enumerationData;

        public ValidationExpressionBuilder(IRecursionPolicy recursionPolicy, IValidatorProvider validatorProvider, IEnumerable<IValidationExpressionConverter> converters)
        {
            _recursionPolicy = recursionPolicy;
            _validatorProvider = validatorProvider;
            _converters = converters.ToArray();

            _typeData = new ConcurrentDictionary<Type, TypeData>();
            _enumerationData = new ConcurrentDictionary<Type, EnumerationData>();
        }

        public ValidationImplementationContext<Func<T, ValidationError?>> Build<T>()
        {
            var root = Expression.Parameter(typeof(T));
            var returnLabel = Expression.Label(typeof(ValidationError?));

            var context = new ValidationContext((member, id) => Expression.Return(returnLabel, Expression.Convert(CreateValidationError(id, member), typeof(ValidationError?))));

            context.Queue.Enqueue(() => GetValidationExpressions(root, context.MemberGraph.RootId, context));

            var validators = DrainQueue(context.Queue).ToArray();

            var returnSite = Expression.Label(returnLabel, Expression.Constant(null, typeof(ValidationError?)));
            var body = validators.Any() ? (Expression)Expression.Block(Expression.Block(validators), returnSite) : Expression.Constant(null, typeof(ValidationError?));

            return CreateValidationContext(Expression.Lambda<Func<T, ValidationError?>>(body, root), context.MemberGraph);
        }

        public ValidationImplementationContext<Action<T, ICollection<ValidationError>>> BuildFull<T>()
        {
            var root = Expression.Parameter(typeof(T));
            var validationErrors = Expression.Parameter(typeof(ICollection<ValidationError>));

            var context = new ValidationContext((member, id) => Expression.Call(validationErrors, Collection_Add, CreateValidationError(id, member)));

            context.Queue.Enqueue(() => GetValidationExpressions(root, context.MemberGraph.RootId, context));

            var validators = DrainQueue(context.Queue).ToArray();

            var body = validators.Any() ? (Expression)Expression.Block(validators) : Expression.Empty();

            return CreateValidationContext(Expression.Lambda<Action<T, ICollection<ValidationError>>>(body, root, validationErrors), context.MemberGraph);
        }

        private static Expression CreateValidationError(int id, Expression member)
        {
            return Expression.New(ValidationError_Ctor, Expression.Constant(id), Expression.Convert(member, typeof(object)));
        }

        private static ValidationImplementationContext<TDelegate> CreateValidationContext<TDelegate>(Expression<TDelegate> expr, MemberGraph memberGraph) where TDelegate : class
        {
            memberGraph.Seal();
            return new ValidationImplementationContext<TDelegate>(expr, memberGraph);
        }

        private IEnumerable<Expression> DrainQueue(ValidationQueue validationQueue)
        {
            while (validationQueue.HasItems)
                foreach (var expr in validationQueue.Produce())
                    yield return expr;
        }

        private IEnumerable<Expression> GetValidationExpressions(Expression root, int rootId, ValidationContext context)
        {
            var typeData = _typeData.GetOrAdd(root.Type, GetTypeData);

            foreach (var converter in typeData.Converters)
                yield return Validate(root, root, rootId, converter, context);

            foreach (var memberData in typeData.Members)
            {
                var member = Expression.MakeMemberAccess(root, memberData.MemberInfo);
                var id = context.MemberGraph.NewMemberId(rootId, memberData.MemberInfo.Name);

                foreach (var converter in memberData.Converters)
                    yield return Validate(root, member, id, converter, context);

                if (!memberData.RecursionPolicy.HasFlag(PolicyOptions.NoFollow))
                    if (IsNullable(member.Type))
                        context.Queue.Enqueue(() => NullCheck(member, GetValidationExpressions(member, id, context)));
                    else
                        context.Queue.Enqueue(() => GetValidationExpressions(member, id, context));

                if (!memberData.RecursionPolicy.HasFlag(PolicyOptions.NoIterate) && memberData.EnumerableTypeParameter != null)
                {
                    var enumerableTypeParameter = memberData.EnumerableTypeParameter;
                    var collectionId = context.MemberGraph.NewMemberId(rootId, memberData.MemberInfo.Name, MemberType.Collection);
                    context.Queue.Enqueue(() => ForEach(member, enumerableTypeParameter, e => NullCheck(e, GetValidationExpressions(e, collectionId, context))));
                }
            }
        }

        private static Expression Validate(Expression root, Expression member, int memberId, ReadyConverter converter, ValidationContext context)
        {
            var validatorId = context.MemberGraph.NewValidatorId(memberId, converter.ValidatorInfo);
            return Expression.IfThen(Expression.Not(converter.Convert(member)), context.GetInvalidValueHandlerExpression(root, validatorId));
        }

        private static IEnumerable<Expression> NullCheck(Expression value, IEnumerable<Expression> body)
        {
            if (body.Any())
                yield return Expression.IfThen(Expression.NotEqual(value, Expression.Constant(null, value.Type)), Expression.Block(body));
        }

        private IEnumerable<Expression> ForEach(Expression member, Type itemType, Func<Expression, IEnumerable<Expression>> bodySelector)
        {
            var enumerationData = _enumerationData.GetOrAdd(itemType, t => new EnumerationData(t));
            var enumerator = Expression.Variable(enumerationData.EnumeratorType);
            var current = Expression.MakeMemberAccess(enumerator, enumerationData.Current);
            var breakLabel = Expression.Label();
            var body = bodySelector(current);

            if (body.Any())
                yield return Expression.Block(
                    new[] { enumerator }, // variable declaration
                    Expression.Assign(enumerator, Expression.Call(member, enumerationData.GetEnumerator)),
                    Expression.TryFinally(
                        Expression.Loop(
                            Expression.IfThenElse(
                                Expression.Call(enumerator, enumerationData.MoveNext),
                                Expression.Block(body),
                                Expression.Break(breakLabel)),
                            breakLabel),
                        Expression.Call(enumerator, enumerationData.Dispose)));
        }

        private static bool IsNullable(Type type)
        {
            return !type.IsValueType || type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        private TypeData GetTypeData(Type type)
        {
            const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty;

            var fields = type.GetFields(MemberFlags).Select(f => new MemberData(f, _recursionPolicy, GetConverters(f, f.FieldType)));
            var properties = type.GetProperties(MemberFlags).Select(p => new MemberData(p, _recursionPolicy, GetConverters(p, p.PropertyType)));

            return new TypeData(type, GetConverters(type, type), fields.Concat(properties).ToArray());
        }

        private IEnumerable<ReadyConverter> GetConverters(MemberInfo member, Type memberType)
        {
            return _validatorProvider
                .GetValidators(member)
                .SelectMany(v => _converters
                    .Where(c => c.CanConvert(v.Validator, memberType))
                    .Select(c => new ReadyConverter(v, c))
                    .Take(1))
                .ToArray();
        }

        private class TypeData
        {
            public TypeData(Type type, IEnumerable<ReadyConverter> converters, IEnumerable<MemberData> members)
            {
                Type = type;
                Converters = converters;
                Members = members;
            }

            public Type Type { get; private set; }
            public IEnumerable<ReadyConverter> Converters { get; private set; }
            public IEnumerable<MemberData> Members { get; private set; }
        }

        private class ValidationContext
        {
            private readonly Func<Expression, int, Expression> _onIsInvalid;

            public ValidationContext(Func<Expression, int, Expression> onIsInvalid)
            {
                _onIsInvalid = onIsInvalid;

                Queue = new ValidationQueue();
                MemberGraph = new MemberGraph();
            }

            public ValidationQueue Queue { get; private set; }

            public MemberGraph MemberGraph { get; private set; }

            public Expression GetInvalidValueHandlerExpression(Expression member, int validatorId)
            {
                return _onIsInvalid(member, validatorId);
            }
        }

        private class MemberData
        {
            public MemberData(FieldInfo fieldInfo, IRecursionPolicy recursionPolicy, IEnumerable<ReadyConverter> converters)
                : this(fieldInfo, fieldInfo.FieldType, recursionPolicy, converters) { }

            public MemberData(PropertyInfo propertyInfo, IRecursionPolicy recursionPolicy, IEnumerable<ReadyConverter> converters)
                : this(propertyInfo, propertyInfo.PropertyType, recursionPolicy, converters) { }

            private MemberData(MemberInfo memberInfo, Type memberType, IRecursionPolicy recursionPolicy, IEnumerable<ReadyConverter> converters)
            {
                MemberInfo = memberInfo;
                RecursionPolicy = recursionPolicy.GetPolicy(memberInfo);
                Converters = converters;
                EnumerableTypeParameter = GetEnumerableTypeParameter(memberType);
            }

            private static Type GetEnumerableTypeParameter(Type type)
            {
                return type
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();
            }

            public MemberInfo MemberInfo { get; private set; }
            public PolicyOptions RecursionPolicy { get; private set; }
            public IEnumerable<ReadyConverter> Converters { get; private set; }
            public Type EnumerableTypeParameter { get; private set; }
        }

        private class EnumerationData
        {
            private static MethodInfo Disposable_Dispose = typeof(IDisposable).GetMethod("Dispose", new Type[0]);
            private static MethodInfo Enumerator_MoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext", new Type[0]);

            public EnumerationData(Type itemType)
            {
                EnumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
                EnumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);

                GetEnumerator = EnumerableType.GetMethod("GetEnumerator", new Type[0]);
                Current = EnumeratorType.GetProperty("Current");
            }

            public Type EnumerableType { get; private set; }
            public Type EnumeratorType { get; private set; }
            public MethodInfo MoveNext { get { return Enumerator_MoveNext; } }
            public MethodInfo Dispose { get { return Disposable_Dispose; } }
            public MethodInfo GetEnumerator { get; private set; }
            public PropertyInfo Current { get; private set; }
        }

        private class ValidationQueue
        {
            private readonly Queue<Func<IEnumerable<Expression>>> _queue = new Queue<Func<IEnumerable<Expression>>>();

            public bool HasItems { get { return _queue.Count > 0; } }

            public void Enqueue(Func<IEnumerable<Expression>> work)
            {
                _queue.Enqueue(work);
            }

            public IEnumerable<Expression> Produce()
            {
                return _queue.Dequeue()();
            }
        }

        private class ReadyConverter
        {
            private readonly ValidatorInfo _valdiatorInfo;
            private readonly IValidationExpressionConverter _converter;

            public ReadyConverter(ValidatorInfo validatorInfo, IValidationExpressionConverter converter)
            {
                _valdiatorInfo = validatorInfo;
                _converter = converter;
            }

            public ValidatorInfo ValidatorInfo { get { return _valdiatorInfo; } }

            public Expression Convert(Expression member)
            {
                return _converter.Convert(_valdiatorInfo.Validator, member);
            }
        }
    }
}
