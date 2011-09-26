using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace CompiledValidators
{
    /// <summary>
    /// Converts <see cref="IDataErrorInfo"/> objects into validation expressions.
    /// </summary>
    public class DataErrorInfoValidationExpressionConverter : IValidationExpressionConverter
    {
        private static readonly MethodInfo String_IsNullOrEmpty = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) });
        private static readonly PropertyInfo DataErrorInfo_Error = typeof(IDataErrorInfo).GetProperty("Error");

        /// <summary>
        /// Converts the specified validator.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <param name="member">The member.</param>
        /// <returns></returns>
        public Expression Convert(object validator, Expression member)
        {
            return Expression.Call(String_IsNullOrEmpty, Expression.MakeMemberAccess(member, DataErrorInfo_Error));
        }

        /// <summary>
        /// Determines whether this instance can convert the specified validator.
        /// </summary>
        /// <param name="validator">The validator.</param>
        /// <param name="memberType">Type of the member.</param>
        /// <returns>
        ///   <c>true</c> if this instance can convert the specified validator; otherwise, <c>false</c>.
        /// </returns>
        public bool CanConvert(object validator, Type memberType)
        {
            return validator == null && typeof(IDataErrorInfo).IsAssignableFrom(memberType);
        }
    }
}
