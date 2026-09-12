using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from a signed 64-bit integer.</summary>
internal sealed class LongTypeConverter :
    ITypeConverter<string, long>,
    ITypeConverter<long, string>,
    ITypeConverter<long, object>,
    ITypeConverter<long, sbyte>,
    ITypeConverter<long, byte>,
    ITypeConverter<long, short>,
    ITypeConverter<long, ushort>,
    ITypeConverter<long, int>,
    ITypeConverter<long, uint>,
    ITypeConverter<long, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out long result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToInt64(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out long result)
    {
        return long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out long result)
    {
        if (input <= long.MaxValue)
        {
            result = (long)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out long result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
