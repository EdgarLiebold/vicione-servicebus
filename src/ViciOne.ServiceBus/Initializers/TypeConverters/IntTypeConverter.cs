using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides an int type converter implementation.
/// </summary>
public class IntTypeConverter :
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
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(byte input, out int result)
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
    public bool TryConvert(long input, out int result)
    {
        result = Convert.ToInt32(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out int result)
    {
        if (input != null)
        {
            result = Convert.ToInt32(input);
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
    public bool TryConvert(sbyte input, out int result)
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
    public bool TryConvert(short input, out int result)
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
    public bool TryConvert(string? input, out int result)
    {
        return int.TryParse(input, out result);
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(uint input, out int result)
    {
        result = Convert.ToInt32(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ulong input, out int result)
    {
        result = Convert.ToInt32(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ushort input, out int result)
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
    public bool TryConvert(int input, out string result)
    {
        result = input.ToString();
        return true;
    }
}
