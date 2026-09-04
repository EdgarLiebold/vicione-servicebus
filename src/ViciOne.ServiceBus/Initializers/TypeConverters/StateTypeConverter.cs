namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a state type converter implementation.
/// </summary>
public class StateTypeConverter :
    ITypeConverter<string, State>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(State? input, out string? result)
    {
        if (input != null)
        {
            result = input.Name;
            return true;
        }

        result = null;
        return false;
    }
}
