using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompiledValidators
{
    /// <summary>
    /// Provides validation using cached static analysis.
    /// </summary>
    public class Validator : IValidator
    {
        private readonly bool _isThreadSafe;
        private readonly Core.ValidationExpressionBuilder _expressionBuilder;
        private readonly Dictionary<Type, object> _nonThreadSafeCache;
        private readonly ConcurrentDictionary<Type, object> _threadSafeCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        /// <param name="isThreadSafe">Set to <c>true</c> to indicate that the validator will be called from a thread-safe context.</param>
        /// <param name="recursionPolicy">The recursion policy.</param>
        /// <param name="validatorProvider">The validator provider.</param>
        /// <param name="converters">The converters.</param>
        public Validator(bool isThreadSafe, IRecursionPolicy recursionPolicy, IValidatorProvider validatorProvider, params IValidationExpressionConverter[] converters)
            : this(isThreadSafe, recursionPolicy, validatorProvider, (IEnumerable<IValidationExpressionConverter>)converters) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        /// <param name="isThreadSafe">Set to <c>true</c> to indicate that the validator will be called from a thread-safe context.</param>
        /// <param name="recursionPolicy">The recursion policy.</param>
        /// <param name="validatorProvider">The validator provider.</param>
        /// <param name="converters">The converters.</param>
        public Validator(bool isThreadSafe, IRecursionPolicy recursionPolicy, IValidatorProvider validatorProvider, IEnumerable<IValidationExpressionConverter> converters)
        {
            _isThreadSafe = isThreadSafe;

            if (isThreadSafe)
                _nonThreadSafeCache = new Dictionary<Type, object>();
            else
                _threadSafeCache = new ConcurrentDictionary<Type, object>();

            _expressionBuilder = new Core.ValidationExpressionBuilder(recursionPolicy, validatorProvider, converters);
        }

        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid<T>(T obj)
        {
            return obj != null && GetValidators<T>().ReturnFirstErrorValidator.Value.Validate(obj) == null;
        }

        /// <summary>
        /// Validates until the first error is found and returns it.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <returns>
        /// The first invalid object encountered in the tree.
        /// </returns>
        public IEnumerable<ValidationError> ValidateToFirstError<T>(T obj)
        {
            if (obj == null) return RootValidationErrors;

            var validator = GetValidators<T>().ReturnFirstErrorValidator.Value;
            var result = validator.Validate(obj);

            if (result == null) return null;

            return validator.MemberGraph
                .GetErrorMessages(result.Value.Id, result.Value.Object)
                .Select(m => new ValidationError(m.MemberName, m.ErrorMessage, result.Value.Object));
        }

        /// <summary>
        /// Validates the specified object and returns all errors.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <param name="isOptimistic">Set to <c>true</c> to indicate high liklihood that the object is valid.</param>
        /// <returns>
        /// A list of errors or <c>null</c>.
        /// </returns>
        public IEnumerable<ValidationError> Validate<T>(T obj, bool isOptimistic = true)
        {
            if (obj == null) return RootValidationErrors;

            var validators = GetValidators<T>();

            if (isOptimistic && validators.ReturnFirstErrorValidator.Value.Validate(obj) == null) return null;

            var results = new List<Core.ValidationError>();
            var collectAllErrosValidator = validators.CollectAllErrosValidator.Value;
            var del = collectAllErrosValidator.Validate;
            
            del.Invoke(obj, results);

            return results.SelectMany(r =>
                collectAllErrosValidator
                .MemberGraph
                .GetErrorMessages(r.Id, r.Object)
                .Select(m => new ValidationError(m.MemberName, m.ErrorMessage, r.Object)));
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

        /// <summary>
        /// Gets the validation error returned if the given object is null.
        /// </summary>
        public static readonly ValidationError RootNullValidationError = new ValidationError(Core.MemberGraph.RootMemberName, "Root object cannot be null.", null);

        private static readonly IEnumerable<ValidationError> RootValidationErrors = new[] { RootNullValidationError };

        /// <summary>
        /// Gets or sets the default validator.
        /// </summary>
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
