using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from Boolean values.</summary>
internal sealed class BooleanTypeConverter :
    ITypeConverter<string, bool>,
    ITypeConverter<bool, string>,
    ITypeConverter<bool, object>,
    ITypeConverter<bool, sbyte>,
    ITypeConverter<bool, byte>,
    ITypeConverter<bool, short>,
    ITypeConverter<bool, ushort>,
    ITypeConverter<bool, int>,
    ITypeConverter<bool, uint>,
    ITypeConverter<bool, long>,
    ITypeConverter<bool, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out bool result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToBoolean(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out bool result)
    {
        return bool.TryParse(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(bool input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
