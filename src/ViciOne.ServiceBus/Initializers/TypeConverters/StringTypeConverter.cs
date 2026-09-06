namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts string type values.</summary>
public class StringTypeConverter :
    ITypeConverter<string, object>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
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
