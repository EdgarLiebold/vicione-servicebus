using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides an enum type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class EnumTypeConverter<T> :
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
    where T : struct
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(byte input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(int input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(long input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out T result)
    {
        if (input is string text)
            return TryConvert(text, out result);

        if (input != null)
        {
            var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));

            if (Enum.IsDefined(typeof(T), value))
            {
                result = (T)Enum.ToObject(typeof(T), value);
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(sbyte input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(short input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out T result)
    {
        return Enum.TryParse(input, true, out result);
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(uint input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ulong input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ushort input, out T result)
    {
        var value = Convert.ChangeType(input, Enum.GetUnderlyingType(typeof(T)));
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default;
        return false;
    }
}
