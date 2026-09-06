using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Configures in memory bus factory.</summary>
public class InMemoryBusFactoryConfigurator :
    BusFactoryConfigurator,
    IInMemoryBusFactoryConfigurator,
    IBusFactory
{
    readonly IInMemoryBusConfiguration _busConfiguration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busConfiguration">The bus configuration.</param>
    public InMemoryBusFactoryConfigurator(IInMemoryBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = true;
    }

    /// <summary>Creates bus endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created bus endpoint configuration.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        return _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    public void Publish<T>(Action<IInMemoryMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IInMemoryMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Applies the host configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Host(Action<IInMemoryHostConfigurator>? configure)
    {
        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Applies the host configuration.</summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure)
    {
        _hostConfiguration.BaseAddress = baseAddress;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Applies the host configuration.</summary>
    /// <param name="virtualHost">The virtual host.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure)
    {
        _hostConfiguration.BaseAddress = new UriBuilder(_hostConfiguration.HostAddress) { Path = virtualHost }.Uri;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Gets the publish topology.</summary>
    public new IInMemoryPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }
}
