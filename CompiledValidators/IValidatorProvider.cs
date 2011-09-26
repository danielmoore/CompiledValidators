using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    /// <summary>
    /// Provides validators for types and their members.
    /// </summary>
    public interface IValidatorProvider
    {
        /// <summary>
        /// Gets the validators for a type or a member.
        /// </summary>
        /// <param name="member">The construct to get validators for.</param>
        /// <returns>A list of validators.</returns>
        /// <remarks>
        /// The list of possible types passed to <paramref name="member"/> are:
        /// <list type="disc">
        /// <item><see cref="Type"/></item>
        /// <itm><see cref="FieldInfo"/></itm>
        /// <item><see cref="PropertyInfo"/></item>
        /// </list>
        /// </remarks>
        IEnumerable<ValidatorInfo> GetValidators(MemberInfo member);
    }
}
