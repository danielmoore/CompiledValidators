namespace CompiledValidators
{
    /// <summary>
    /// Represents an error message associated with a member.
    /// </summary>
    public struct MemberValidationErrorMessage
    {
        private readonly string _memberName;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberValidationErrorMessage"/> struct.
        /// </summary>
        /// <param name="memberName">The name of the member the error message is associated with.</param>
        /// <param name="errorMessage">The error message.</param>
        public MemberValidationErrorMessage(string memberName, string errorMessage)
        {
            _memberName = memberName;
            _errorMessage = errorMessage;
        }

        /// <summary>
        /// The name of the member the error message is associated with.
        /// </summary>
        public string MemberName { get { return _memberName; } }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get { return _errorMessage; } }
    }
}
