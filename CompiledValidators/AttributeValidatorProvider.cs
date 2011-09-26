using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CompiledValidators
{
    /// <summary>
    /// Provides all attributes as validators.
    /// </summary>
    public class AttributeValidatorProvider : IValidatorProvider
    {
        /// <summary>
        /// Gets the validators for a type or a member.
        /// </summary>
        /// <param name="member">The construct to get validators for.</param>
        /// <returns>
        /// A list of validators.
        /// </returns>
        public IEnumerable<ValidatorInfo> GetValidators(MemberInfo member)
        {
            return member.GetCustomAttributes(false).Select(x => new ValidatorInfo(x));
        }
    }
}
