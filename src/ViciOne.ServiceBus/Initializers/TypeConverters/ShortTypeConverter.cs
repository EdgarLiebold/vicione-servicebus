using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from a signed 16-bit integer.</summary>
internal sealed class ShortTypeConverter :
    ITypeConverter<string, short>,
    ITypeConverter<short, string>,
    ITypeConverter<short, object>,
    ITypeConverter<short, sbyte>,
    ITypeConverter<short, byte>,
    ITypeConverter<short, ushort>,
    ITypeConverter<short, int>,
    ITypeConverter<short, uint>,
    ITypeConverter<short, long>,
    ITypeConverter<short, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out short result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out short result)
    {
        if (input is >= short.MinValue and <= short.MaxValue)
        {
            result = (short)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out short result)
    {
        if (input is >= short.MinValue and <= short.MaxValue)
        {
            result = (short)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out short result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToInt16(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out short result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out short result)
    {
        return short.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out short result)
    {
        if (input <= short.MaxValue)
        {
            result = (short)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out short result)
    {
        if (input <= (ulong)short.MaxValue)
        {
            result = (short)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out short result)
    {
        if (input <= short.MaxValue)
        {
            result = (short)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
