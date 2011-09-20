using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    public interface IValidatorProvider
    {
        IEnumerable<object> GetValidators(MemberInfo member);
    }
}
