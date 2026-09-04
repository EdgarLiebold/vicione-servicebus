namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a string type converter implementation.
/// </summary>
public class StringTypeConverter :
    ITypeConverter<string, object>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out string? result)
    {
        if (input != null)
        {
            result = input.ToString();
            return true;
        }

        result = null;
        return false;
    }
}
