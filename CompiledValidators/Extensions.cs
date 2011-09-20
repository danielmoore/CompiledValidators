using System;
using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    public static class Extensions
    {
        private static readonly MethodInfo Validator_IsValid = typeof(IValidator).GetMethod("IsValid");
        private static readonly MethodInfo Validator_Validate = typeof(IValidator).GetMethod("Validate");
        private static readonly MethodInfo Validator_ValidateToFirstError = typeof(IValidator).GetMethod("ValidateToFirstError");

        public static bool IsValidInferred(this IValidator source, object obj)
        {
            return Invoke<bool>(source, Validator_IsValid, obj.GetType(), obj);
        }

        public static IEnumerable<ValidationError> ValidateInferred<T>(this IValidator source, T obj, bool isOptimistic = true)
        {
            return Invoke<IEnumerable<ValidationError>>(source, Validator_Validate, obj.GetType(), obj, isOptimistic);
        }

        public static ValidationError ValidateToFirstErrorInferred<T>(this IValidator source, T obj)
        {
            return Invoke<ValidationError>(source, Validator_ValidateToFirstError, obj.GetType(), obj);
        }

        private static T Invoke<T>(object target, MethodInfo method, Type genericParameter, params object[] args)
        {
            return (T)method.MakeGenericMethod(genericParameter).Invoke(target, args);
        }
    }
}
