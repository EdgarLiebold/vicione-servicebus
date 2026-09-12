using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts offset-aware date/time values and signed Unix millisecond timestamps.</summary>
internal sealed class DateTimeOffsetTypeConverter :
    ITypeConverter<string, DateTimeOffset>,
    ITypeConverter<int, DateTimeOffset>,
    ITypeConverter<long, DateTimeOffset>,
    ITypeConverter<DateTimeOffset, string>,
    ITypeConverter<DateTimeOffset, object>,
    ITypeConverter<DateTimeOffset, DateTime>,
    ITypeConverter<DateTimeOffset, int>,
    ITypeConverter<DateTimeOffset, long>
{
    /// <inheritdoc />
    public bool TryConvert(object? input, out DateTimeOffset result)
    {
        switch (input)
        {
            case DateTime dateTime:
                return TryConvert(dateTime, out result);

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

    /// <inheritdoc />
    public bool TryConvert(string? input, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out result);
    }

    /// <summary>Converts a UTC or local value as its represented instant and treats an unspecified value as UTC.</summary>
    /// <param name="input">The date/time value.</param>
    /// <param name="result">Receives the offset-aware value.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public bool TryConvert(DateTime input, out DateTimeOffset result)
    {
        result = input.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(input, DateTimeKind.Utc))
            : new DateTimeOffset(input);
        return true;
    }

    /// <summary>Converts a signed 32-bit Unix millisecond timestamp to its UTC instant.</summary>
    /// <param name="input">The Unix millisecond timestamp.</param>
    /// <param name="result">Receives the UTC instant.</param>
    /// <returns>Always <see langword="true"/> because every 32-bit timestamp is representable.</returns>
    public bool TryConvert(int input, out DateTimeOffset result)
    {
        result = DateTimeOffset.FromUnixTimeMilliseconds(input);
        return true;
    }

    /// <summary>Attempts to convert a signed 64-bit Unix millisecond timestamp to its UTC instant.</summary>
    /// <param name="input">The Unix millisecond timestamp.</param>
    /// <param name="result">Receives the UTC instant when the timestamp is in range.</param>
    /// <returns><see langword="true"/> when the timestamp is representable; otherwise, <see langword="false"/>.</returns>
    public bool TryConvert(long input, out DateTimeOffset result)
    {
        try
        {
            result = DateTimeOffset.FromUnixTimeMilliseconds(input);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(DateTimeOffset input, out int result)
    {
        long milliseconds = input.ToUnixTimeMilliseconds();
        if (milliseconds is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)milliseconds;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(DateTimeOffset input, out long result)
    {
        result = input.ToUnixTimeMilliseconds();
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(DateTimeOffset input, out string result)
    {
        result = input.ToString("O", CultureInfo.InvariantCulture);
        return true;
    }
}
