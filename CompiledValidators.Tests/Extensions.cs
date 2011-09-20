using System;
using System.Linq.Expressions;
using Moq;

namespace CompiledValidators.Tests
{
    public static class Extensions
    {
        public static bool Execute<T>(this IValidationExpressionConverter converter, object validator, T arg)
        {
            var param = Expression.Parameter(typeof(T));

            return Expression.Lambda<Func<T, bool>>(converter.Convert(validator, param), param).Compile()(arg);
        }

        public static Mock<T> GetMock<T>(this T obj) where T : class
        {
            return Mock.Get(obj);
        }
    }
}
