using System;
using System.Collections.Generic;
namespace CompiledValidators
{
    /// <summary>
    /// Represents a validator with static or validator-specific error messages.
    /// </summary>
    public class ErrorMessageValidatorInfo : ValidatorInfo
    {
        private readonly Func<string> _errorMessageFactory;
        private string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessageValidatorInfo"/> class.
        /// </summary>
        /// <param name="validator">The validator this object describes.</param>
        /// <param name="errorMessage">The static error message.</param>
        public ErrorMessageValidatorInfo(object validator, string errorMessage)
            : base(validator)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");

            _errorMessage = errorMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessageValidatorInfo"/> class.
        /// </summary>
        /// <param name="validator">The validator this object describes.</param>
        /// <param name="errorMessage">A function that provides the error message for this validator.</param>
        public ErrorMessageValidatorInfo(object validator, Func<string> errorMessage)
            : base(validator)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");

            _errorMessageFactory = errorMessage;
        }

        /// <summary>
        /// Gets the error message for this validator.
        /// </summary>
        /// <returns>An error message.</returns>
        public string GetErrorMessage()
        {
            if (_errorMessage == null)
                lock (_errorMessageFactory)
                    if (_errorMessage == null)
                        _errorMessage = _errorMessageFactory();

            return _errorMessage;
        }
    }

    /// <summary>
    /// Represents a validator with instance-specific error messages.
    /// </summary>
    public class MemberErrorValidatorInfo : ValidatorInfo
    {
        private readonly Func<object, IEnumerable<MemberValidationErrorMessage>> _errorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberErrorValidatorInfo"/> class.
        /// </summary>
        /// <param name="validator">The validator this object describes.</param>
        /// <param name="errors">A function that given an invalid object, provides the errors specific to this validator.</param>
        public MemberErrorValidatorInfo(object validator, Func<object, IEnumerable<MemberValidationErrorMessage>> errors)
            : base(validator)
        {
            if (errors == null) throw new ArgumentNullException("errors");
            _errorFactory = errors;
        }

        /// <summary>
        /// Gets the error messages this validator produces.
        /// </summary>
        /// <param name="obj">The object that is being validated.</param>
        /// <returns>A list of errors this validator has produced.</returns>
        public IEnumerable<MemberValidationErrorMessage> GetErrorMessages(object obj)
        {
            return _errorFactory(obj);
        }
    }

    /// <summary>
    /// Represents a validator with no error message.
    /// </summary>
    public class ValidatorInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatorInfo"/> class.
        /// </summary>
        /// <param name="validator">The validator this object describes.</param>
        public ValidatorInfo(object validator)
        {
            Validator = validator;
        }

        /// <summary>
        /// Gets the validator this object describes.
        /// </summary>
        public object Validator { get; private set; }
    }
}
