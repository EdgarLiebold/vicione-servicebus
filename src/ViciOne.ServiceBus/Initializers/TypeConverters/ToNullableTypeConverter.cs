namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a to nullable type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ToNullableTypeConverter<T> :
    ITypeConverter<T?, T>
    where T : struct
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T input, out T? result)
    {
        result = input;
        return true;
    }
}


/// <summary>
/// Provides a to nullable type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class ToNullableTypeConverter<T, TInput> :
    ITypeConverter<T?, TInput>
    where T : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="typeConverter">The type converter value.</param>
    public ToNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
    {
        _typeConverter = typeConverter;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(TInput? input, out T? result)
    {
        if (_typeConverter.TryConvert(input, out var intermediateValue))
        {
            result = intermediateValue;
            return true;
        }

        result = default;
        return false;
    }
}
