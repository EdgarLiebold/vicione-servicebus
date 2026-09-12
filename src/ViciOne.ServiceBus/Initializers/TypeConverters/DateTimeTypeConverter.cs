using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts date/time values and signed Unix millisecond timestamps to UTC instants.</summary>
internal sealed class DateTimeTypeConverter :
    ITypeConverter<string, DateTime>,
    ITypeConverter<int, DateTime>,
    ITypeConverter<long, DateTime>,
    ITypeConverter<DateTime, string>,
    ITypeConverter<DateTime, object>,
    ITypeConverter<DateTime, DateTimeOffset>,
    ITypeConverter<DateTime, int>,
    ITypeConverter<DateTime, long>
{
    public bool TryConvert(DateTimeOffset input, out DateTime result)
    {
        result = input.UtcDateTime;
        return true;
    }

    public bool TryConvert(int input, out DateTime result)
    {
        return TryFromUnixMilliseconds(input, out result);
    }

    public bool TryConvert(long input, out DateTime result)
    {
        return TryFromUnixMilliseconds(input, out result);
    }

    public bool TryConvert(object? input, out DateTime result)
    {
        switch (input)
        {
            case DateTime dateTime:
                result = NormalizeToUtc(dateTime);
                return true;

            case DateTimeOffset dateTimeOffset:
                result = dateTimeOffset.UtcDateTime;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                return TryConvert(text, out result);

            default:
                result = default;
                return false;
        }
    }

    public bool TryConvert(string? input, out DateTime result)
    {
        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var value))
        {
            result = value.UtcDateTime;
            return true;
        }

        result = default;
        return false;
    }

    public bool TryConvert(DateTime input, out int result)
    {
        DateTime utc = NormalizeToUtc(input);
        long milliseconds = new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        if (milliseconds is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)milliseconds;
            return true;
        }

        result = default;
        return false;
    }

    public bool TryConvert(DateTime input, out long result)
    {
        DateTime utc = NormalizeToUtc(input);
        result = new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        return true;
    }

    public bool TryConvert(DateTime input, out string result)
    {
        result = input.ToString("O", CultureInfo.InvariantCulture);
        return true;
    }

    static DateTime NormalizeToUtc(DateTime input) => input.Kind switch
    {
        DateTimeKind.Utc => input,
        DateTimeKind.Local => input.ToUniversalTime(),
        _ => DateTime.SpecifyKind(input, DateTimeKind.Utc),
    };

    static bool TryFromUnixMilliseconds(long input, out DateTime result)
    {
        try
        {
            result = DateTimeOffset.FromUnixTimeMilliseconds(input).UtcDateTime;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = default;
            return false;
        }
    }
}
