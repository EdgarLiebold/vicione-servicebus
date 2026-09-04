using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a byte type converter implementation.
/// </summary>
public class ByteTypeConverter :
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
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(int input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(long input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out byte result)
    {
        if (input != null)
        {
            result = Convert.ToByte(input);
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
    public bool TryConvert(sbyte input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(short input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out byte result)
    {
        return byte.TryParse(input, out result);
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(uint input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ulong input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ushort input, out byte result)
    {
        result = Convert.ToByte(input);
        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(byte input, out string result)
    {
        result = input.ToString();
        return true;
    }
}
