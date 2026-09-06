using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures an in-memory bus, its host identity, and transport topology.</summary>
public interface IInMemoryBusFactoryConfigurator :
    IBusFactoryConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Gets the in-memory publish topology.</summary>
    new IInMemoryPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures in-memory publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">An optional callback that configures publish topology.</param>
    void Publish<T>(Action<IInMemoryMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Configures in-memory publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures publish topology.</param>
    void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>Configures the current in-memory host.</summary>
    /// <param name="configure">An optional callback that configures the host.</param>
    void Host(Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Sets and configures the in-memory host base address.</summary>
    /// <param name="baseAddress">The base address for the in-memory host.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Sets a virtual-host path that distinguishes in-memory bus instances.</summary>
    /// <param name="virtualHost">The virtual-host path.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure = null);
}
