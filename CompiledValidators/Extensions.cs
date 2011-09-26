using System;
using System.Collections.Generic;
using System.Reflection;

namespace CompiledValidators
{
    /// <summary>
    /// Extensions for validation constructs.
    /// </summary>
    public static class Extensions
    {
        private static readonly MethodInfo Validator_IsValid = typeof(IValidator).GetMethod("IsValid");
        private static readonly MethodInfo Validator_Validate = typeof(IValidator).GetMethod("Validate");
        private static readonly MethodInfo Validator_ValidateToFirstError = typeof(IValidator).GetMethod("ValidateToFirstError");

        /// <summary>
        /// Calls <see cref="IValidator.IsValid"/> with the given object's inspected type as the type parameter.
        /// </summary>
        /// <param name="source">The validator to call.</param>
        /// <param name="obj">The object to validate.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidInferred(this IValidator source, object obj)
        {
            return Invoke<bool>(source, Validator_IsValid, obj.GetType(), obj);
        }

        /// <summary>
        /// Calls <see cref="IValidator.Validate"/> with the given object's inspected type as the type parameter.
        /// </summary>
        /// <param name="source">The validator to call.</param>
        /// <param name="obj">The object to validate.</param>
        /// <param name="isOptimistic">Set to <c>true</c> to indicate high liklihood that the object is valid.</param>
        /// <returns>A list of errors or <c>null</c>.</returns>
        public static IEnumerable<ValidationError> ValidateInferred(this IValidator source, object obj, bool isOptimistic = true)
        {
            return Invoke<IEnumerable<ValidationError>>(source, Validator_Validate, obj.GetType(), obj, isOptimistic);
        }

        /// <summary>
        /// Calls <see cref="IValidator.ValidateToFirstError"/> with the given object's inspected type as the type parameter.
        /// </summary>
        /// <param name="source">The validator to call.</param>
        /// <param name="obj">The object to validate.</param>
        /// <returns>The first invalid object encountered in the tree.</returns>
        public static IEnumerable<ValidationError> ValidateToFirstErrorInferred(this IValidator source, object obj)
        {
            return Invoke<IEnumerable<ValidationError>>(source, Validator_ValidateToFirstError, obj.GetType(), obj);
        }

        private static T Invoke<T>(object target, MethodInfo method, Type genericParameter, params object[] args)
        {
            return (T)method.MakeGenericMethod(genericParameter).Invoke(target, args);
        }
    }
}
