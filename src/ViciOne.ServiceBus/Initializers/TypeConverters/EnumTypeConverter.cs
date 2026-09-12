using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts defined enum values from their names and supported integral representations.</summary>
/// <typeparam name="T">The enum type.</typeparam>
internal sealed class EnumTypeConverter<T> :
    ITypeConverter<T, string>,
    ITypeConverter<T, object>,
    ITypeConverter<T, sbyte>,
    ITypeConverter<T, byte>,
    ITypeConverter<T, short>,
    ITypeConverter<T, ushort>,
    ITypeConverter<T, int>,
    ITypeConverter<T, uint>,
    ITypeConverter<T, long>,
    ITypeConverter<T, ulong>
    where T : struct, Enum
{
    /// <inheritdoc />
    public bool TryConvert(byte input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(int input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(long input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out T result)
    {
        if (input is string text)
            return TryConvert(text, out result);

        return input == null ? ReturnFalse(out result) : TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(sbyte input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(short input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out T result)
    {
        if (Enum.TryParse(input, true, out result) && Enum.IsDefined(result))
            return true;

        result = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryConvert(uint input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(ulong input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(ushort input, out T result)
    {
        return TryConvertNumeric(input, out result);
    }

    static bool TryConvertNumeric(object input, out T result)
    {
        try
        {
            object value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)), System.Globalization.CultureInfo.InvariantCulture);
            if (Enum.IsDefined(typeof(T), value))
            {
                result = (T)Enum.ToObject(typeof(T), value);
                return true;
            }
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
        }

        result = default;
        return false;
    }

    static bool ReturnFalse(out T result)
    {
        result = default;
        return false;
    }
}
