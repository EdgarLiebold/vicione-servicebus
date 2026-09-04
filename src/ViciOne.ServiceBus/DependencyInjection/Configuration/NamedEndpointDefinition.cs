namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a named endpoint definition implementation.
/// </summary>
public class NamedEndpointDefinition :
    DefaultEndpointDefinition
{
    readonly string _endpointName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointName">The endpoint name value.</param>
    public NamedEndpointDefinition(string endpointName)
    {
        _endpointName = endpointName;
    }

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public override string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointName;
    }
}
