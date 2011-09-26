using System;
using System.Reflection;

namespace CompiledValidators
{
    /// <summary>
    /// Indicates options to be applied when recursing the the object tree.
    /// </summary>
    [Flags]
    public enum PolicyOptions
    {
        /// <summary>
        /// Use default behavior.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Do not validate the members of the current member.
        /// </summary>
        NoFollow = 0x1,

        /// <summary>
        /// Do not iterate the current member and validate the items.
        /// </summary>
        NoIterate = 0x2,

        /// <summary>
        /// All options.
        /// </summary>
        All = 0x3
    }

    /// <summary>
    /// Defines the policy to be used when recursing the object tree.
    /// </summary>
    public interface IRecursionPolicy
    {
        /// <summary>
        /// Gets the policy to be applied.
        /// </summary>
        /// <param name="member">The member to which the policy will be applied.</param>
        /// <returns>A policy.</returns>
        PolicyOptions GetPolicy(MemberInfo member);
    }
}
