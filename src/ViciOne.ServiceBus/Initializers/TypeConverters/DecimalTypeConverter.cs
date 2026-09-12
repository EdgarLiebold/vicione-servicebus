using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from decimal values.</summary>
internal sealed class DecimalTypeConverter :
    ITypeConverter<string, decimal>,
    ITypeConverter<decimal, string>,
    ITypeConverter<decimal, object>,
    ITypeConverter<decimal, sbyte>,
    ITypeConverter<decimal, byte>,
    ITypeConverter<decimal, short>,
    ITypeConverter<decimal, ushort>,
    ITypeConverter<decimal, int>,
    ITypeConverter<decimal, uint>,
    ITypeConverter<decimal, long>,
    ITypeConverter<decimal, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out decimal result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToDecimal(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out decimal result)
    {
        return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out decimal result)
    {
        result = Convert.ToDecimal(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(decimal input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
