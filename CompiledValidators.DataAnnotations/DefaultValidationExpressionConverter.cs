using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace CompiledValidators.DataAnnotations
{
    public class DefaultValidationExpressionConverter : IValidationExpressionConverter
    {
        private static readonly MethodInfo ValidationAttribute_IsValid = typeof(ValidationAttribute).GetMethod("IsValid", new[] { typeof(object) });
        public Expression Convert(object validator, Expression member)
        {
            return Expression.Call(Expression.Constant(validator, typeof(ValidationAttribute)), ValidationAttribute_IsValid, member);
        }

        public bool CanConvert(object validator, Type memberType)
        {
            return validator is ValidationAttribute;
        }
    }
}
