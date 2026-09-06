namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts to object type values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ToObjectTypeConverter<T> :
    ITypeConverter<object, T>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T? input, out object? result)
    {
        result = input;
        return true;
    }
}
