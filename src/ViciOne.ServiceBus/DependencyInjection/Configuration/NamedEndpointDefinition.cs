namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for named endpoint.</summary>
public class NamedEndpointDefinition :
    DefaultEndpointDefinition
{
    readonly string _endpointName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    public NamedEndpointDefinition(string endpointName)
    {
        _endpointName = endpointName;
    }

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public override string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return _endpointName;
    }
}
