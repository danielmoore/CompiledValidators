using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;

namespace CompiledValidators.DataAnnotations
{
    public class ValidatableObjectValidationExpressionConverter : IValidationExpressionConverter
    {
        private static readonly MethodInfo ValidatableObjectValidationExpressionConverter_Validate = 
            typeof(ValidatableObjectValidationExpressionConverter).GetMethod("Validate", new[] { typeof(IValidatableObject) });

        public Expression Convert(object validator, Expression member)
        {
            return Expression.Call(ValidatableObjectValidationExpressionConverter_Validate, member);
        }

        public bool CanConvert(object validator, Type memberType)
        {
            return validator == null && typeof(IValidatableObject).IsAssignableFrom(memberType);
        }

        public static bool Validate(IValidatableObject target)
        {
            return !target.Validate(new ValidationContext(target, null, null)).Any();
        }
    }
}
