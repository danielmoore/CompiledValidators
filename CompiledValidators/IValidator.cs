using System.Collections.Generic;

namespace CompiledValidators
{
    public interface IValidator
    {
        bool IsValid<T>(T obj);
        IEnumerable<ValidationError> Validate<T>(T obj, bool isOptimistic = true);
        ValidationError ValidateToFirstError<T>(T obj);
    }
}
