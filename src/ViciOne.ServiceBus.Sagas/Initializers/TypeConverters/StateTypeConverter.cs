namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts state type values.</summary>
public class StateTypeConverter :
    ITypeConverter<string, IState>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(IState? input, out string? result)
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
