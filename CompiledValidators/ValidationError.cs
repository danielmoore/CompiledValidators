﻿namespace CompiledValidators
{
    public struct ValidationError
    {
        private readonly string _memberName;
        private readonly string _errorMessage;
        private readonly object _object;

        public ValidationError(string memberName, string errorMessage, object @object)
        {
            _memberName = memberName;
            _errorMessage = errorMessage;
            _object = @object;
        }

        public string MemberName { get { return _memberName; } }

        public string ErrorMessage { get { return _errorMessage; } }

        public object Object { get { return _object; } }

        public static readonly ValidationError Empty;
    }
}
