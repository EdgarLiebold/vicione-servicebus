using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a short type converter implementation.
/// </summary>
public class ShortTypeConverter :
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
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(byte input, out short result)
    {
        result = input;
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(int input, out short result)
    {
        result = Convert.ToInt16(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(long input, out short result)
    {
        result = Convert.ToInt16(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out short result)
    {
        if (input != null)
        {
            result = Convert.ToInt16(input);
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
    public bool TryConvert(sbyte input, out short result)
    {
        result = input;
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out short result)
    {
        return short.TryParse(input, out result);
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(uint input, out short result)
    {
        result = Convert.ToInt16(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ulong input, out short result)
    {
        result = Convert.ToInt16(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ushort input, out short result)
    {
        result = Convert.ToInt16(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(short input, out string result)
    {
        result = input.ToString();
        return true;
    }
}
