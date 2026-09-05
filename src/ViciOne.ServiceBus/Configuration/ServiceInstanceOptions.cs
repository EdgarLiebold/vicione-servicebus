namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines configuration options for service instance.
/// </summary>
public sealed class ServiceInstanceOptions :
    OptionsSet
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ServiceInstanceOptions()
    {
        EndpointNameFormatter = DefaultEndpointNameFormatter.Instance;
    }

    /// <summary>
    /// Gets or sets the endpoint name formatter value.
    /// </summary>
    public IEndpointNameFormatter EndpointNameFormatter { get; private set; }

    /// <summary>
    /// Sets endpoint name formatter.
    /// </summary>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public ServiceInstanceOptions SetEndpointNameFormatter(IEndpointNameFormatter endpointNameFormatter)
    {
        EndpointNameFormatter = endpointNameFormatter ?? throw new ConfigurationException(
            "Service instance for bus 'default': EndpointNameFormatter must not be null. Supply an endpoint name formatter.");

        return this;
    }
}
