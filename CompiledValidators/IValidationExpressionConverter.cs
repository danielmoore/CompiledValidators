using System;
using System.Linq.Expressions;

namespace CompiledValidators
{
    public interface IValidationExpressionConverter
    {
        Expression Convert(object validator, Expression member);
        bool CanConvert(object validator, Type memberType);
    }
}
