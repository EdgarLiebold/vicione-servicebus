using System;
using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts supported scalar representations to and from double-precision values.</summary>
internal sealed class DoubleTypeConverter :
    ITypeConverter<string, double>,
    ITypeConverter<double, string>,
    ITypeConverter<double, object>,
    ITypeConverter<double, sbyte>,
    ITypeConverter<double, byte>,
    ITypeConverter<double, short>,
    ITypeConverter<double, ushort>,
    ITypeConverter<double, int>,
    ITypeConverter<double, uint>,
    ITypeConverter<double, long>,
    ITypeConverter<double, ulong>
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out double result)
    {
        if (input == null)
        {
            result = default;
            return false;
        }

        try
        {
            result = Convert.ToDouble(input, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out double result)
    {
        return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out double result)
    {
        result = Convert.ToDouble(input);
        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(double input, out string result)
    {
        result = input.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
