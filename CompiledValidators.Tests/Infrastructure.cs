using System;
using System.Linq.Expressions;

namespace CompiledValidators.Tests.Infrastructure
{
    public class ValidationExpressionConverter : IValidationExpressionConverter
    {
        public Expression Convert(object validator, System.Linq.Expressions.Expression member)
        {
            return validator is ValidAttribute ? Expression.Constant(true) : Expression.Constant(false);
        }

        public bool CanConvert(object validator, Type memberType)
        {
            return validator is ValidAttribute || validator is InvalidAttribute;
        }
    }

    public class ValidAttribute : Attribute { }
    public class InvalidAttribute : Attribute { }
}
