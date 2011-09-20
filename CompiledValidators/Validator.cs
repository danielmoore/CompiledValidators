using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompiledValidators
{
    public class Validator : CompiledValidators.IValidator
    {
        private readonly bool _isThreadSafe;
        private readonly Core.ValidationExpressionBuilder _expressionBuilder;
        private readonly Dictionary<Type, object> _nonThreadSafeCache;
        private readonly ConcurrentDictionary<Type, object> _threadSafeCache;

        public Validator(bool isThreadSafe, IValidatorProvider validatorProvider, params IValidationExpressionConverter[] converters)
            : this(isThreadSafe, validatorProvider, (IEnumerable<IValidationExpressionConverter>)converters) { }

        public Validator(bool isThreadSafe, IValidatorProvider validatorProvider, IEnumerable<IValidationExpressionConverter> converters)
        {
            _isThreadSafe = isThreadSafe;

            if (isThreadSafe)
                _nonThreadSafeCache = new Dictionary<Type, object>();
            else
                _threadSafeCache = new ConcurrentDictionary<Type, object>();

            _expressionBuilder = new Core.ValidationExpressionBuilder(validatorProvider, converters);
        }

        public bool IsValid<T>(T obj)
        {
            return obj != null && GetValidators<T>().ReturnFirstErrorValidator.Value.Validate(obj) == null;
        }

        public ValidationError ValidateToFirstError<T>(T obj)
        {
            if (obj == null) return RootValidationError;

            var validator = GetValidators<T>().ReturnFirstErrorValidator.Value;
            var result = validator.Validate(obj);

            return result == null ? null : new ValidationError(validator.MemberGraph.GetMemberAccessString(result.Value.Id), result.Value.Object);
        }

        public IEnumerable<ValidationError> Validate<T>(T obj, bool isOptimistic = true)
        {
            if (obj == null) return new[] { RootValidationError };

            var validators = GetValidators<T>();

            if (isOptimistic && validators.ReturnFirstErrorValidator.Value.Validate(obj) == null) return null;

            var results = new List<Core.ValidationError>();
            var collectAllErrosValidator = validators.CollectAllErrosValidator.Value;
            collectAllErrosValidator.Validate(obj, results);

            return results.Select(r => new ValidationError(collectAllErrosValidator.MemberGraph.GetMemberAccessString(r.Id), r.Object));
        }

        private Validators<T> GetValidators<T>()
        {
            object validators;
            if (_isThreadSafe)
            {
                if (!_nonThreadSafeCache.TryGetValue(typeof(T), out validators))
                    _nonThreadSafeCache.Add(typeof(T), validators = new Validators<T>(_expressionBuilder, _isThreadSafe));
            }
            else
                validators = _threadSafeCache.GetOrAdd(typeof(T), t => new Validators<T>(_expressionBuilder, _isThreadSafe));

            return (Validators<T>)validators;
        }

        public static readonly ValidationError RootValidationError = new ValidationError("root", null);

        public static Validator Default { get; set; }

        private class Validators<T>
        {
            public Validators(Core.ValidationExpressionBuilder expressionBuilder, bool isThreadSafe)
            {
                var threadSafteyMode = isThreadSafe ? LazyThreadSafetyMode.None : LazyThreadSafetyMode.ExecutionAndPublication;
                ReturnFirstErrorValidator = new Lazy<Core.ValidationImplementationContext<Func<T, Core.ValidationError?>>>(expressionBuilder.Build<T>, threadSafteyMode);
                CollectAllErrosValidator = new Lazy<Core.ValidationImplementationContext<Action<T, ICollection<Core.ValidationError>>>>(expressionBuilder.BuildFull<T>, threadSafteyMode);
            }

            public Lazy<Core.ValidationImplementationContext<Func<T, Core.ValidationError?>>> ReturnFirstErrorValidator { get; private set; }

            public Lazy<Core.ValidationImplementationContext<Action<T, ICollection<Core.ValidationError>>>> CollectAllErrosValidator { get; private set; }
        }
    }
}
