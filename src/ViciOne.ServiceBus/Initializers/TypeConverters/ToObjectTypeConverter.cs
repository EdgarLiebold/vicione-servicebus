namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Exposes a typed value through an object result without changing its runtime identity.</summary>
/// <typeparam name="T">The source value type.</typeparam>
internal sealed class ToObjectTypeConverter<T> :
    ITypeConverter<object, T>
{
    /// <inheritdoc />
    public bool TryConvert(T? input, out object? result)
    {
        result = input;
        return true;
    }
}
