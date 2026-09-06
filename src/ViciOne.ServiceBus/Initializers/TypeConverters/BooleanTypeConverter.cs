using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts boolean type values.</summary>
public class BooleanTypeConverter :
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
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(byte input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(int input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(long input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out bool result)
    {
        if (input != null)
        {
            result = Convert.ToBoolean(input);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(sbyte input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(short input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out bool result)
    {
        return bool.TryParse(input, out result);
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(uint input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ulong input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(ushort input, out bool result)
    {
        result = Convert.ToBoolean(input);
        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(bool input, out string result)
    {
        result = input.ToString();
        return true;
    }
}
