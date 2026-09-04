using System;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a date time offset type converter implementation.
/// </summary>
public class DateTimeOffsetTypeConverter :
    ITypeConverter<string, DateTimeOffset>,
    ITypeConverter<int, DateTimeOffset>,
    ITypeConverter<long, DateTimeOffset>,
    ITypeConverter<DateTimeOffset, string>,
    ITypeConverter<DateTimeOffset, object>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out DateTimeOffset result)
    {
        switch (input)
        {
            case DateTime dateTime:
                result = dateTime;
                return true;

            case DateTimeOffset dateTimeOffset:
                result = dateTimeOffset;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                return TryConvert(text, out result);

            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(input, out result);
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(DateTimeOffset input, out int result)
    {
        if (input >= DateTimeConstants.Epoch)
        {
            var timeSpan = input - DateTimeConstants.Epoch;
            if (timeSpan.TotalMilliseconds <= int.MaxValue)
            {
                result = (int)timeSpan.TotalMilliseconds;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(DateTimeOffset input, out long result)
    {
        if (input >= DateTimeConstants.Epoch)
        {
            var timeSpan = input - DateTimeConstants.Epoch;
            if (timeSpan.TotalMilliseconds <= long.MaxValue)
            {
                result = (long)timeSpan.TotalMilliseconds;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(DateTimeOffset input, out string result)
    {
        result = input.ToString("O");
        return true;
    }
}
