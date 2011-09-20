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
        private static readonly Assembly Mscorlib = typeof(object).Assembly;
        private static readonly MethodInfo Collection_Add = typeof(ICollection<ValidationError>).GetMethod("Add", new[] { typeof(ValidationError) });
        private static readonly ConstructorInfo ValidationError_Ctor = typeof(ValidationError).GetConstructor(new[] { typeof(int), typeof(object) });

        private readonly IValidatorProvider _validatorProvider;
        private readonly IEnumerable<IValidationExpressionConverter> _converters;

        private readonly ConcurrentDictionary<Type, TypeData> _typeData;
        private readonly ConcurrentDictionary<Type, EnumerationData> _enumerationData;

        public ValidationExpressionBuilder(IValidatorProvider validatorProvider, IEnumerable<IValidationExpressionConverter> converters)
        {
            _validatorProvider = validatorProvider;
            _converters = converters.ToArray();

            _typeData = new ConcurrentDictionary<Type, TypeData>();
            _enumerationData = new ConcurrentDictionary<Type, EnumerationData>();
        }

        public ValidationImplementationContext<Func<T, ValidationError?>> Build<T>()
        {
            var queue = new ValidationQueue();
            var root = Expression.Parameter(typeof(T));
            var returnLabel = Expression.Label(typeof(ValidationError?));

            var memberGraph = new MemberGraph();

            queue.Enqueue(() => GetValidationExpressions(queue, root, memberGraph.RootId, memberGraph,
                (id, member) => Expression.Return(returnLabel, Expression.Convert(CreateValidationError(id, member), typeof(ValidationError?)))));

            var validators = DrainQueue(queue).ToArray();

            var returnSite = Expression.Label(returnLabel, Expression.Constant(null, typeof(ValidationError?)));
            var body = validators.Any() ? (Expression)Expression.Block(Expression.Block(validators), returnSite) : Expression.Constant(null, typeof(ValidationError?));

            return CreateValidationContext(Expression.Lambda<Func<T, ValidationError?>>(body, root), memberGraph);
        }

        public ValidationImplementationContext<Action<T, ICollection<ValidationError>>> BuildFull<T>()
        {
            var queue = new ValidationQueue();
            var root = Expression.Parameter(typeof(T));
            var validationErrors = Expression.Parameter(typeof(ICollection<ValidationError>));

            var memberGraph = new MemberGraph();

            queue.Enqueue(() => GetValidationExpressions(queue, root, memberGraph.RootId, memberGraph,
                (id, member) => Expression.Call(validationErrors, Collection_Add, CreateValidationError(id, member))));

            var validators = DrainQueue(queue).ToArray();

            var body = validators.Any() ? (Expression)Expression.Block(validators) : Expression.Empty();

            return CreateValidationContext(Expression.Lambda<Action<T, ICollection<ValidationError>>>(body, root, validationErrors), memberGraph);
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

        private IEnumerable<Expression> GetValidationExpressions(ValidationQueue validationQueue, Expression root, int rootId, MemberGraph memberGraph, Func<int, Expression, Expression> onIsInvalid)
        {
            var typeData = _typeData.GetOrAdd(root.Type, GetTypeData);

            if (typeData.Converters.Any())
                yield return Validate(root, root, rootId, typeData.Converters, onIsInvalid);

            foreach (var memberData in typeData.Members)
            {
                var member = Expression.MakeMemberAccess(root, memberData.MemberInfo);
                var id = memberGraph.NewId(rootId, memberData.MemberInfo);

                if (memberData.Converters.Any())
                    yield return Validate(root, member, id, memberData.Converters, onIsInvalid);

                if (member.Type.Assembly != Mscorlib)
                    if (IsNullable(member.Type))
                        validationQueue.Enqueue(() => NullCheck(member, GetValidationExpressions(validationQueue, member, id, memberGraph, onIsInvalid)));
                    else
                        validationQueue.Enqueue(() => GetValidationExpressions(validationQueue, member, id, memberGraph, onIsInvalid));

                if (memberData.EnumerableTypeParameter != null)
                {
                    var enumerableTypeParameter = memberData.EnumerableTypeParameter;
                    var collectionId = memberGraph.NewId(rootId, memberData.MemberInfo, MemberType.Collection);
                    validationQueue.Enqueue(() => ForEach(member, enumerableTypeParameter, e => NullCheck(e, GetValidationExpressions(validationQueue, e, collectionId, memberGraph, onIsInvalid))));
                }
            }
        }

        private static Expression Validate(Expression root, Expression member, int id, IEnumerable<ReadyConverter> converters, Func<int, Expression, Expression> onIsInvalid)
        {
            var isValidExpression = converters.Select(c => c.Convert(member)).Aggregate(Expression.AndAlso);
            return Expression.IfThen(Expression.Not(isValidExpression), onIsInvalid(id, root));
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
            var fields = type.GetFields().Select(f => new MemberData(f, GetConverters(f, f.FieldType)));
            var properties = type.GetProperties().Select(p => new MemberData(p, GetConverters(p, p.PropertyType)));

            return new TypeData(type, GetConverters(type, type).ToArray(), fields.Concat(properties).ToArray());
        }

        private IEnumerable<ReadyConverter> GetConverters(MemberInfo member, Type memberType)
        {
            return _validatorProvider
                .GetValidators(member)
                .SelectMany(a => _converters
                    .Where(c => c.CanConvert(a, memberType))
                    .Select(c => new ReadyConverter(a, c))
                    .Take(1));
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

        private class MemberData
        {
            public MemberData(FieldInfo fieldInfo, IEnumerable<ReadyConverter> converters)
            {
                MemberInfo = fieldInfo;
                Converters = converters;
                EnumerableTypeParameter = GetEnumerableTypeParameter(fieldInfo.FieldType);
            }

            public MemberData(PropertyInfo propertyInfo, IEnumerable<ReadyConverter> converters)
            {
                MemberInfo = propertyInfo;
                Converters = converters;
                EnumerableTypeParameter = GetEnumerableTypeParameter(propertyInfo.PropertyType);
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
            private readonly object _validator;
            private readonly IValidationExpressionConverter _converter;

            public ReadyConverter(object validator, IValidationExpressionConverter converter)
            {
                _validator = validator;
                _converter = converter;
            }

            public Expression Convert(Expression member)
            {
                return _converter.Convert(_validator, member);
            }
        }
    }
}
