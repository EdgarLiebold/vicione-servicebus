using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Defines the topology for in memory bus.</summary>
public class InMemoryBusTopology :
    BusTopology,
    IInMemoryBusTopology
{
    readonly IInMemoryTopologyConfiguration _configuration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The callback used to configure the component.</param>
    public InMemoryBusTopology(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The in memory message publish topology produced by the operation.</returns>
    public new IInMemoryMessagePublishTopology<T> Publish<T>()
        where T : class
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }
}
