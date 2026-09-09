using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates configuration-validation results and composes their member paths.</summary>
public static class ValidationResultExtensions
{
    /// <summary>Creates a failure associated with a configuration specification.</summary>
    /// <param name="specification">The specification whose validation produced the failure.</param>
    /// <param name="message">The validation failure message.</param>
    /// <returns>The failure result.</returns>
    public static ValidationResult Failure(this ISpecification specification, string message)
    {
        return new Result(ValidationResultDisposition.Failure, message);
    }

    /// <summary>Creates a failure for a configuration member.</summary>
    /// <param name="specification">The specification whose validation produced the failure, when one is available.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="message">The validation failure message.</param>
    /// <returns>The failure result.</returns>
    public static ValidationResult Failure(this ISpecification? specification, string key, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, message);
    }

    /// <summary>Creates a failure for a configuration member and its rejected value.</summary>
    /// <param name="specification">The specification whose validation produced the failure.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="value">The rejected value.</param>
    /// <param name="message">The validation failure message.</param>
    /// <returns>The failure result.</returns>
    public static ValidationResult Failure(this ISpecification specification, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Failure, key, value, message);
    }

    /// <summary>Creates a warning associated with a configuration specification.</summary>
    /// <param name="specification">The specification whose validation produced the warning.</param>
    /// <param name="message">The validation warning message.</param>
    /// <returns>The warning result.</returns>
    public static ValidationResult Warning(this ISpecification specification, string message)
    {
        return new Result(ValidationResultDisposition.Warning, message);
    }

    /// <summary>Creates a warning for a configuration member.</summary>
    /// <param name="specification">The specification whose validation produced the warning, when one is available.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="message">The validation warning message.</param>
    /// <returns>The warning result.</returns>
    public static ValidationResult Warning(this ISpecification? specification, string key, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, message);
    }

    /// <summary>Creates a warning for a configuration member and its noteworthy value.</summary>
    /// <param name="specification">The specification whose validation produced the warning.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="value">The noteworthy value.</param>
    /// <param name="message">The validation warning message.</param>
    /// <returns>The warning result.</returns>
    public static ValidationResult Warning(this ISpecification specification, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Warning, key, value, message);
    }

    /// <summary>Creates a success associated with a configuration specification.</summary>
    /// <param name="specification">The specification whose validation produced the success.</param>
    /// <param name="message">The validation success message.</param>
    /// <returns>The success result.</returns>
    public static ValidationResult Success(this ISpecification specification, string message)
    {
        return new Result(ValidationResultDisposition.Success, message);
    }

    /// <summary>Creates a success for a configuration member.</summary>
    /// <param name="specification">The specification whose validation produced the success.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="message">The validation success message.</param>
    /// <returns>The success result.</returns>
    public static ValidationResult Success(this ISpecification specification, string key, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, message);
    }

    /// <summary>Creates a success for a configuration member and its accepted value.</summary>
    /// <param name="specification">The specification whose validation produced the success.</param>
    /// <param name="key">The configuration-member path.</param>
    /// <param name="value">The accepted value.</param>
    /// <param name="message">The validation success message.</param>
    /// <returns>The success result.</returns>
    public static ValidationResult Success(this ISpecification specification, string key, string value, string message)
    {
        return new Result(ValidationResultDisposition.Success, key, value, message);
    }

    /// <summary>Prefixes a validation result's member path with its parent configuration path.</summary>
    /// <param name="result">The result whose member path is nested.</param>
    /// <param name="parentKey">The parent configuration path.</param>
    /// <returns>A result containing the composed member path.</returns>
    public static ValidationResult WithParentKey(this ValidationResult result, string parentKey)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);

        var key = string.IsNullOrEmpty(result.Key)
            ? parentKey
            : parentKey + "." + result.Key;

        return new Result(result.Disposition, key, result.Value, result.Message);
    }

    sealed class Result :
        ValidationResult
    {
        public Result(ValidationResultDisposition disposition, string key, string? value, string message)
        {
            Validate(disposition, message);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            Disposition = disposition;
            Key = key;
            Value = value;
            Message = message;
        }

        public Result(ValidationResultDisposition disposition, string key, string message)
        {
            Validate(disposition, message);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            Disposition = disposition;
            Key = key;
            Message = message;
        }

        public Result(ValidationResultDisposition disposition, string message)
        {
            Validate(disposition, message);

            Key = string.Empty;
            Disposition = disposition;
            Message = message;
        }

        public ValidationResultDisposition Disposition { get; }
        public string Key { get; }
        public string? Value { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"[{Disposition}] {(string.IsNullOrEmpty(Key) ? Message : Key + " " + Message)}";
        }

        static void Validate(ValidationResultDisposition disposition, string message)
        {
            if (!Enum.IsDefined(disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition), disposition, "The validation disposition must be defined.");

            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }
    }
}
