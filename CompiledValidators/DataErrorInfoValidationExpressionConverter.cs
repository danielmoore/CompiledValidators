using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace CompiledValidators
{
    public class DataErrorInfoValidationExpressionConverter : IValidationExpressionConverter
    {
        private static readonly MethodInfo String_IsNullOrEmpty = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) });
        private static readonly PropertyInfo DataErrorInfo_Error = typeof(IDataErrorInfo).GetProperty("Error");

        public Expression Convert(object validator, Expression member)
        {
            return Expression.Call(String_IsNullOrEmpty, Expression.MakeMemberAccess(member, DataErrorInfo_Error));
        }

        public bool CanConvert(object validator, Type memberType)
        {
            return validator == null && typeof(IDataErrorInfo).IsAssignableFrom(memberType);
        }
    }
}
