namespace CompiledValidators
{
    public struct MemberValidationErrorMessage
    {
        private readonly string _memberName;
        private readonly string _errorMessage;

        public MemberValidationErrorMessage(string memberName, string errorMessage)
        {
            _memberName = memberName;
            _errorMessage = errorMessage;
        }

        public string MemberName { get { return _memberName; } }

        public string ErrorMessage { get { return _errorMessage; } }

        public static readonly MemberValidationErrorMessage Empty;
    }
}
