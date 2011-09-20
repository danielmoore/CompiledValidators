namespace CompiledValidators
{
    public sealed class ValidationError
    {
        private readonly string _member;
        private readonly object _object;

        public ValidationError(string member, object @object)
        {
            _member = member;
            _object = @object;
        }

        public string Member { get { return _member; } }
        public object Object { get { return _object; } }
    }
}
