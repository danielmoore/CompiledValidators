using System;
using System.Collections.Generic;
namespace CompiledValidators
{
    public class ErrorMessageValidatorInfo : ValidatorInfo
    {
        private readonly Func<string> _errorMessageFactory;
        private string _errorMessage;

        public ErrorMessageValidatorInfo(object validator, string errorMessage)
            : base(validator)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");

            _errorMessage = errorMessage;
        }

        public ErrorMessageValidatorInfo(object validator, Func<string> errorMessage)
            : base(validator)
        {
            if (errorMessage == null) throw new ArgumentNullException("errorMessage");

            _errorMessageFactory = errorMessage;
        }

        public string GetErrorMessage()
        {
            if (_errorMessage == null)
                lock (_errorMessageFactory)
                    if (_errorMessage == null)
                        _errorMessage = _errorMessageFactory();

            return _errorMessage;
        }
    }

    public class MemberErrorValidatorInfo : ValidatorInfo
    {
        private readonly Func<object, IEnumerable<MemberValidationErrorMessage>> _errorFactory;

        public MemberErrorValidatorInfo(object validator, Func<object, IEnumerable<MemberValidationErrorMessage>> errors)
            : base(validator)
        {
            if (errors == null) throw new ArgumentNullException("errors");
            _errorFactory = errors;
        }

        public IEnumerable<MemberValidationErrorMessage> GetErrorMessages(object obj)
        {
            return _errorFactory(obj);
        }
    }

    /// <summary>
    /// Represents a validator and its associated error message.
    /// </summary>
    public class ValidatorInfo
    {
        public ValidatorInfo(object validator)
        {
            Validator = validator;
        }

        public object Validator { get; private set; }
    }
}
