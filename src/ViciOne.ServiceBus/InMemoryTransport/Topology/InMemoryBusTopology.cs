using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Connects bus-level topology access to the in-memory topology configuration.</summary>
internal sealed class InMemoryBusTopology :
    BusTopology,
    IInMemoryBusTopology
{
    readonly IInMemoryTopologyConfiguration _configuration;

    /// <summary>Creates bus topology over a host and mutable topology configuration.</summary>
    /// <param name="hostConfiguration">The owning host configuration.</param>
    /// <param name="configuration">The in-memory topology configuration.</param>
    public InMemoryBusTopology(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration configuration)
        : base(
            hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration)),
            configuration ?? throw new ArgumentNullException(nameof(configuration)))
    {
        _configuration = configuration;
    }

    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    public new IInMemoryMessagePublishTopology<TMessage> Publish<TMessage>()
        where TMessage : class
    {
        return _configuration.Publish.GetMessageTopology<TMessage>() as IInMemoryMessagePublishTopology<TMessage>
            ?? throw new InvalidOperationException($"The publish topology for {TypeCache<TMessage>.ShortName} is not an in-memory topology.");
    }
}
