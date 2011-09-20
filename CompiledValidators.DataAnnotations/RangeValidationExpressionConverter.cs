using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using TypeConverter = System.Convert;

namespace CompiledValidators.DataAnnotations
{
    public class RangeValidationExpressionConverter : IValidationExpressionConverter
    {
        private static readonly Type[] ConvertibleTypes =
            new[] { typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) };

        public Expression Convert(object validator, Expression member)
        {
            var rangeAttr = (RangeAttribute)validator;
            if (rangeAttr.OperandType.Equals(member.Type))
                return Expression.AndAlso(
                    Expression.LessThanOrEqual(
                        Expression.Constant(TypeConverter.ChangeType(rangeAttr.Minimum, rangeAttr.OperandType), rangeAttr.OperandType),
                        member),
                    Expression.LessThanOrEqual(
                        member,
                        Expression.Constant(TypeConverter.ChangeType(rangeAttr.Maximum, rangeAttr.OperandType), rangeAttr.OperandType)));
            else
                return Expression.Constant(true);
        }

        public bool CanConvert(object validator, Type memberType)
        {
            var rangeAttr = validator as RangeAttribute;

            return rangeAttr != null && rangeAttr.OperandType.Equals(memberType) && ConvertibleTypes.Contains(memberType);
        }
    }
}
