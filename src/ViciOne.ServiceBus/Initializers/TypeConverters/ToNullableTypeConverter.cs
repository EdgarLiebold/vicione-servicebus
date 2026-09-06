namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts to nullable type values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ToNullableTypeConverter<T> :
    ITypeConverter<T?, T>
    where T : struct
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T input, out T? result)
    {
        result = input;
        return true;
    }
}


/// <summary>Converts to nullable type values.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class ToNullableTypeConverter<T, TInput> :
    ITypeConverter<T?, TInput>
    where T : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="typeConverter">The type converter.</param>
    public ToNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
    {
        _typeConverter = typeConverter;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
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
