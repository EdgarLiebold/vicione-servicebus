using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for validation result.</summary>
public static class ValidationResultExtensions
{
    /// <summary>Creates a failed result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Failure(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Failure, message);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Failure(this ISpecification? configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, message);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Failure(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, value, message);
    }

    /// <summary>Reports the current warning.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Warning(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Warning, message);
    }

    /// <summary>Reports the current warning.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Warning(this ISpecification? configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, message);
    }

    /// <summary>Reports the current warning.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Warning(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, value, message);
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string message)
    {
        return new Result(ValidationResultDisposition.Success, message);
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string key, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, message);
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult Success(this ISpecification configurator, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, value, message);
    }

    /// <summary>Associates the value with its parent key.</summary>
    /// <param name="result">The result.</param>
    /// <param name="parentKey">The parent key.</param>
    /// <returns>The validation result produced by the operation.</returns>
    public static ValidationResult WithParentKey(this ValidationResult result, string parentKey)
    {
        var key = parentKey + "." + result.Key;

        return new Result(result.Disposition, key, result.Value, result.Message);
    }

    /// <summary>Represents a configuration validation result created by these extensions.</summary>
    public class Result :
        ValidationResult
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="disposition">The disposition.</param>
        /// <param name="key">The key used to identify the requested entry.</param>
        /// <param name="value">The value to process.</param>
        /// <param name="message">The message to process.</param>
        public Result(ValidationResultDisposition disposition, string key, string? value, string message)
        {
            Disposition = disposition;
            Key = key;
            Value = value;
            Message = message;
        }

        /// <summary>Initializes a new instance.</summary>
        /// <param name="disposition">The disposition.</param>
        /// <param name="key">The key used to identify the requested entry.</param>
        /// <param name="message">The message to process.</param>
        public Result(ValidationResultDisposition disposition, string key, string message)
        {
            Disposition = disposition;
            Key = key;
            Message = message;
        }

        /// <summary>Initializes a new instance.</summary>
        /// <param name="disposition">The disposition.</param>
        /// <param name="message">The message to process.</param>
        public Result(ValidationResultDisposition disposition, string message)
        {
            Key = "";
            Disposition = disposition;
            Message = message;
        }

        /// <summary>Gets the disposition.</summary>
        public ValidationResultDisposition Disposition { get; }
        /// <summary>Gets the key.</summary>
        public string Key { get; }
        /// <summary>Gets the value.</summary>
        public string? Value { get; }
        /// <summary>Gets the message.</summary>
        public string Message { get; }

        /// <summary>Returns the string representation of this instance.</summary>
        /// <returns>The converted string.</returns>
        public override string ToString()
        {
            return $"[{Disposition}] {(string.IsNullOrEmpty(Key) ? Message : Key + " " + Message)}";
        }
    }
}
