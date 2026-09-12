using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from an unsigned 8-bit integer.</summary>
internal sealed class ByteTypeConverter :
    ITypeConverter<string, byte>,
    ITypeConverter<byte, string>,
    ITypeConverter<byte, object>,
    ITypeConverter<byte, sbyte>,
    ITypeConverter<byte, short>,
    ITypeConverter<byte, ushort>,
    ITypeConverter<byte, int>,
    ITypeConverter<byte, uint>,
    ITypeConverter<byte, long>,
    ITypeConverter<byte, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(int input, out byte result)
    {
        if (input is >= byte.MinValue and <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out byte result)
    {
        if (input is >= byte.MinValue and <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out byte result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToByte(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out byte result)
    {
        if (input >= 0)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out byte result)
    {
        if (input is >= byte.MinValue and <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out byte result)
    {
        return byte.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out byte result)
    {
        if (input <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out byte result)
    {
        if (input <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out byte result)
    {
        if (input <= byte.MaxValue)
        {
            result = (byte)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(byte input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
