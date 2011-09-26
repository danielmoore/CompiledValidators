using System.Collections.Generic;

namespace CompiledValidators
{
    /// <summary>
    /// Provides validation for objects.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is valid; otherwise, <c>false</c>.
        /// </returns>
        bool IsValid<T>(T obj);

        /// <summary>
        /// Validates the specified object and returns all errors.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <param name="isOptimistic">Set to <c>true</c> to indicate high liklihood that the object is valid.</param>
        /// <returns>A list of errors or <c>null</c>.</returns>
        /// <remarks>
        /// If the <paramref name="isOptimistic"/> flag is set to <c>true</c>, the validator will choose
        /// a faster validation strategy and if the object is valid, return <c>null</c>. If the object is invalid
        /// or the <paramref name="isOptimistic"/> flag is set to <c>false</c>, the validator will use a slower and more thorough
        /// strategy to collect all errors.
        /// </remarks>
        IEnumerable<ValidationError> Validate<T>(T obj, bool isOptimistic = true);

        /// <summary>
        /// Validates until the first error is found and returns it.
        /// </summary>
        /// <typeparam name="T">The type to use for static analysis.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <returns>The first invalid object encountered in the tree.</returns>
        IEnumerable<ValidationError> ValidateToFirstError<T>(T obj);
    }
}
