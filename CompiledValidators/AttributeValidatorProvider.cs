using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    public class AttributeValidatorProvider : IValidatorProvider
    {
        public IEnumerable<object> GetValidators(MemberInfo member)
        {
            return member.GetCustomAttributes(false);
        }
    }
}
