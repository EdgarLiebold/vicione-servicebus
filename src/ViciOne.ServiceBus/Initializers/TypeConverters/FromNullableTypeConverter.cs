namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Returns a nullable value or the value type's default when it is absent.</summary>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class FromNullableTypeConverter<T> :
    ITypeConverter<T, T?>
    where T : struct
{
    /// <inheritdoc />
    public bool TryConvert(T? input, out T result)
    {
        result = input ?? default;
        return true;
    }
}


/// <summary>Converts a nullable source value through its underlying value converter.</summary>
/// <typeparam name="T">The result value type.</typeparam>
/// <typeparam name="TInput">The underlying source value type.</typeparam>
internal sealed class FromNullableTypeConverter<T, TInput> :
    ITypeConverter<T, TInput?>
    where TInput : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>Creates a nullable-input adapter for <paramref name="typeConverter" />.</summary>
    /// <param name="typeConverter">The converter applied to the supplied or default source value.</param>
    public FromNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
    {
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
    public bool TryConvert(TInput? input, out T? result)
    {
        return _typeConverter.TryConvert(input ?? default, out result);
    }
}
