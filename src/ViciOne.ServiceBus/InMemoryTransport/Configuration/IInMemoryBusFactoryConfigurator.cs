using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures in memory bus factory.</summary>
public interface IInMemoryBusFactoryConfigurator :
    IBusFactoryConfigurator<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Gets the publish topology.</summary>
    new IInMemoryPublishTopologyConfigurator PublishTopology { get; }

    /// <summary>Configure the send topology of the message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    void Publish<T>(Action<IInMemoryMessagePublishTopologyConfigurator<T>>? configureTopology = null)
        where T : class;

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null);

    /// <summary>Configure the base address for the host.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Host(Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Configure the base address for the host.</summary>
    /// <param name="baseAddress">The base address for the in-memory host.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure = null);

    /// <summary>Configure the virtual host, to differentiate in-memory bus instances.</summary>
    /// <param name="virtualHost">The virtual host path.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure = null);
}
