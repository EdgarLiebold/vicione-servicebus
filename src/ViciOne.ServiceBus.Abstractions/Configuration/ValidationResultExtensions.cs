using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for validation result.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Performs the failure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Failure(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Failure, message);
    }

    /// <summary>
    /// Performs the failure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Failure(this ISpecification? configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, message);
    }

    /// <summary>
    /// Performs the failure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Failure(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, value, message);
    }

    /// <summary>
    /// Performs the warning operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Warning(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Warning, message);
    }

    /// <summary>
    /// Performs the warning operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Warning(this ISpecification? configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, message);
    }

    /// <summary>
    /// Performs the warning operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Warning(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, value, message);
    }

    /// <summary>
    /// Performs the success operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Success, message);
    }

    /// <summary>
    /// Performs the success operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, message);
    }

    /// <summary>
    /// Performs the success operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, value, message);
    }

    /// <summary>
    /// Performs the with parent key operation.
    /// </summary>
    /// <param name="result">The result value.</param>
    /// <param name="parentKey">The parent key value.</param>
    /// <returns>The result of the operation.</returns>
    public static ValidationResult WithParentKey(this ValidationResult result, string parentKey)
    {
        var key = parentKey + "." + result.Key;

        return new Result(result.Disposition, key, result.Value, result.Message);
    }

    /// <summary>
    /// Provides a result implementation.
    /// </summary>
    public class Result :
        ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="disposition">The disposition value.</param>
        /// <param name="key">The key value.</param>
        /// <param name="value">The value.</param>
        /// <param name="message">The message value.</param>
        public Result(ValidationResultDisposition disposition, string key, string? value, string message)
        {
            Disposition = disposition;
            Key = key;
            Value = value;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="disposition">The disposition value.</param>
        /// <param name="key">The key value.</param>
        /// <param name="message">The message value.</param>
        public Result(ValidationResultDisposition disposition, string key, string message)
        {
            Disposition = disposition;
            Key = key;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="disposition">The disposition value.</param>
        /// <param name="message">The message value.</param>
        public Result(ValidationResultDisposition disposition, string message)
        {
            Key = "";
            Disposition = disposition;
            Message = message;
        }

        /// <summary>
        /// Gets the disposition value.
        /// </summary>
        public ValidationResultDisposition Disposition { get; }
        /// <summary>
        /// Gets the key value.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Gets the underlying value.
        /// </summary>
        public string? Value { get; }
        /// <summary>
        /// Gets the message value.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Returns the string representation of this instance.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override string ToString()
        {
            return $"[{Disposition}] {(string.IsNullOrEmpty(Key) ? Message : Key + " " + Message)}";
        }
    }
}
