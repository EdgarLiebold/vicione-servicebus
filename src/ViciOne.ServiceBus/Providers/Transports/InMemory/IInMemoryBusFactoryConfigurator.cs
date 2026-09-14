using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures an in-memory bus, its host identity, and transport topology.</summary>
public interface IInMemoryBusFactoryConfigurator :
    IBusFactoryConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Gets the in-memory publish topology.</summary>
    new IInMemoryPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configures in-memory publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="configureTopology">An optional callback that configures publish topology.</param>
    void Publish<TMessage>(Action<IInMemoryMessagePublishTopologyConfigurator<TMessage>>? configureTopology = null)
        where TMessage : class;

    /// <summary>Configures in-memory publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures publish topology.</param>
    void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>Configures settings for the current in-memory host.</summary>
    /// <param name="configure">An optional callback that configures host capacity.</param>
    void Host(Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Sets the host identity from an absolute loopback address and applies host settings.</summary>
    /// <param name="baseAddress">The absolute <c>loopback</c> address for the host.</param>
    /// <param name="configure">An optional callback that configures host capacity.</param>
    void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Sets a virtual-host identity that isolates this bus's message fabric and applies host settings.</summary>
    /// <param name="virtualHost">The non-empty virtual-host identity encoded as one address component.</param>
    /// <param name="configure">An optional callback that configures host capacity.</param>
    void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure = null);
}
