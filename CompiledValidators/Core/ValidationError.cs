
namespace CompiledValidators.Core
{
    internal struct ValidationError
    {
        private readonly int _id;
        private readonly object _object;

        public ValidationError(int id, object @object)
        {
            _id = id;
            _object = @object;
        }

        public int Id { get { return _id; } }
        public object Object { get { return _object; } }
    }
}
