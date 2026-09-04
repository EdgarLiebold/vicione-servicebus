namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a to object type converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ToObjectTypeConverter<T> :
    ITypeConverter<object, T>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(T? input, out object? result)
    {
        result = input;
        return true;
    }
}
