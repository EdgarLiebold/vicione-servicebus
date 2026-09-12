using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from a signed 32-bit integer.</summary>
internal sealed class IntTypeConverter :
    ITypeConverter<string, int>,
    ITypeConverter<int, object>,
    ITypeConverter<int, string>,
    ITypeConverter<int, sbyte>,
    ITypeConverter<int, byte>,
    ITypeConverter<int, short>,
    ITypeConverter<int, ushort>,
    ITypeConverter<int, uint>,
    ITypeConverter<int, long>,
    ITypeConverter<int, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out int result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out int result)
    {
        if (input is >= int.MinValue and <= int.MaxValue)
        {
            result = (int)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out int result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToInt32(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out int result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out int result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out int result)
    {
        return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out int result)
    {
        if (input <= int.MaxValue)
        {
            result = (int)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out int result)
    {
        if (input <= int.MaxValue)
        {
            result = (int)input;
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out int result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
