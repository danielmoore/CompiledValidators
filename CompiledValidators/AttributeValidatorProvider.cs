using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CompiledValidators
{
    public class AttributeValidatorProvider : IValidatorProvider
    {
        public IEnumerable<ValidatorInfo> GetValidators(MemberInfo member)
        {
            return member.GetCustomAttributes(false).Select(x => new ValidatorInfo(x));
        }
    }
}
