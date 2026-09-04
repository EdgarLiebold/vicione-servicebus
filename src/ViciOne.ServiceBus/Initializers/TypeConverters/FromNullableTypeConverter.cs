namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a from nullable type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class FromNullableTypeConverter<T> :
    ITypeConverter<T, T?>
    where T : struct
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T? input, out T result)
    {
        result = input ?? default;
        return true;
    }
}


/// <summary>
/// Provides a from nullable type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class FromNullableTypeConverter<T, TInput> :
    ITypeConverter<T, TInput?>
    where TInput : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="typeConverter">The type converter value.</param>
    public FromNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
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
        return _typeConverter.TryConvert(input ?? default, out result);
    }
}
