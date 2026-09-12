namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Wraps a value type in its nullable form.</summary>
/// <typeparam name="T">The underlying value type.</typeparam>
internal sealed class ToNullableTypeConverter<T> :
    ITypeConverter<T?, T>
    where T : struct
{
    /// <inheritdoc />
    public bool TryConvert(T input, out T? result)
    {
        result = input;
        return true;
    }
}


/// <summary>Converts a source value and wraps the result in nullable form.</summary>
/// <typeparam name="T">The underlying result type.</typeparam>
/// <typeparam name="TInput">The source value type.</typeparam>
internal sealed class ToNullableTypeConverter<T, TInput> :
    ITypeConverter<T?, TInput>
    where T : struct
{
    readonly ITypeConverter<T, TInput> _typeConverter;

    /// <summary>Creates a nullable adapter for <paramref name="typeConverter" />.</summary>
    /// <param name="typeConverter">The converter that produces the underlying value.</param>
    public ToNullableTypeConverter(ITypeConverter<T, TInput> typeConverter)
    {
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
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
