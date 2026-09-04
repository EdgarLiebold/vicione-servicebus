using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory bus topology implementation.
/// </summary>
public class InMemoryBusTopology :
    BusTopology,
    IInMemoryBusTopology
{
    readonly IInMemoryTopologyConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public InMemoryBusTopology(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public new IInMemoryMessagePublishTopology<T> Publish<T>()
        where T : class
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }
}
