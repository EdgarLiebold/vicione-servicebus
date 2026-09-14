namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines an endpoint with an explicit name and otherwise default settings.</summary>
internal sealed class NamedEndpointDefinition :
    DefaultEndpointDefinition
{
    readonly string _endpointName;

    /// <summary>Creates an endpoint definition with an explicit name and default settings.</summary>
    /// <param name="endpointName">The receive-endpoint name.</param>
    public NamedEndpointDefinition(string endpointName)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException("Endpoint name must not be empty.", nameof(endpointName));

        _endpointName = endpointName;
    }

    /// <summary>Returns the explicit endpoint name.</summary>
    /// <param name="formatter">The required naming convention; the explicit name is returned unchanged.</param>
    /// <returns>The explicit endpoint name.</returns>
    public override string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        return _endpointName;
    }
}
