namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts from nullable type values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FromNullableTypeConverter<T> :
    ITypeConverter<T, T?>
    where T : struct
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T? input, out T result)
    {
        result = input ?? default;
        return true;
    }
}


/// <summary>Converts from nullable type values.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FromNullableTypeConverter<T, TInput> :
    ITypeConverter<T, TInput?>
    where TInput : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="typeConverter">The type converter.</param>
    public FromNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
    {
        _typeConverter = typeConverter;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(TInput? input, out T? result)
    {
        return _typeConverter.TryConvert(input ?? default, out result);
    }
}
