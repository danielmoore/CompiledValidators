using System;
using System.Linq.Expressions;

namespace CompiledValidators
{
    /// <summary>
    /// Converts a validator into an expression to be called during validation.
    /// </summary>
    public interface IValidationExpressionConverter
    {
        /// <summary>
        /// Converts the specified validator into an expression.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <param name="member">The member being validated.</param>
        /// <returns>An expression that returns <c>true</c> if the object is valid, or <c>false</c> otherwise.</returns>
        Expression Convert(object validator, Expression member);

        /// <summary>
        /// Determines whether this converter can conver the given validator into an expression.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <param name="memberType">The type of the member being validated.</param>
        /// <returns>
        ///   <c>true</c> if this instance can convert the given validator; otherwise, <c>false</c>.
        /// </returns>
        bool CanConvert(object validator, Type memberType);
    }
}
