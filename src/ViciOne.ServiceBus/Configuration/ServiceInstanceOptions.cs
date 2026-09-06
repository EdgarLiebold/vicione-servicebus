namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration options for service instance.</summary>
public sealed class ServiceInstanceOptions :
    OptionsSet
{
    /// <summary>Initializes a new instance.</summary>
    public ServiceInstanceOptions()
    {
        EndpointNameFormatter = DefaultEndpointNameFormatter.Instance;
    }

    /// <summary>Gets or sets the endpoint name formatter.</summary>
    public IEndpointNameFormatter EndpointNameFormatter { get; private set; }

    /// <summary>Sets endpoint name formatter.</summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <returns>The service instance options produced by the operation.</returns>
    public ServiceInstanceOptions SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        EndpointNameFormatter = endpointNameFormatter ?? throw new ConfigurationException(
            "Service instance for bus 'default': EndpointNameFormatter must not be null. Supply an endpoint name formatter.");

        return this;
    }
}
