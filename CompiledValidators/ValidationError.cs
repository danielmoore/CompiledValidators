namespace CompiledValidators
{
    /// <summary>
    /// Represents a specific validation error.
    /// </summary>
    public struct ValidationError
    {
        private readonly string _memberName;
        private readonly string _errorMessage;
        private readonly object _object;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> struct.
        /// </summary>
        /// <param name="memberName">The name of the member that failed validation.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="object">The validation target.</param>
        public ValidationError(string memberName, string errorMessage, object @object)
        {
            _memberName = memberName;
            _errorMessage = errorMessage;
            _object = @object;
        }

        /// <summary>
        /// Gets the name of the member that failed validation.
        /// </summary>
        public string MemberName { get { return _memberName; } }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get { return _errorMessage; } }

        /// <summary>
        /// Gets the validation target.
        /// </summary>
        /// <remarks>
        /// This is the object containing the object that failed validation.
        /// </remarks>
        public object Object { get { return _object; } }
    }
}
