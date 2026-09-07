using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Builds an in-memory bus from host, endpoint, and topology configuration.</summary>
public class InMemoryBusFactoryConfigurator :
    BusFactoryConfigurator,
    IInMemoryBusFactoryConfigurator,
    IBusFactory
{
    readonly IInMemoryBusConfiguration _busConfiguration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Initializes a configurator for an in-memory bus.</summary>
    /// <param name="busConfiguration">The mutable configuration assembled for the bus.</param>
    public InMemoryBusFactoryConfigurator(IInMemoryBusConfiguration busConfiguration)
        : base(busConfiguration ?? throw new ArgumentNullException(nameof(busConfiguration)))
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = true;
    }

    /// <summary>Creates the temporary receive-endpoint configuration used by the bus itself.</summary>
    /// <param name="configure">The callback that configures the bus endpoint.</param>
    /// <returns>The configured bus receive endpoint.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        return _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Configures in-memory publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">An optional callback that configures publish topology.</param>
    public void Publish<T>(Action<IInMemoryMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IInMemoryMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures in-memory publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures publish topology.</param>
    public void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Configures the current in-memory host.</summary>
    /// <param name="configure">An optional callback that configures the host.</param>
    public void Host(Action<IInMemoryHostConfigurator>? configure)
    {
        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Sets and configures the in-memory host base address.</summary>
    /// <param name="baseAddress">The base address for the in-memory host.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        _hostConfiguration.BaseAddress = baseAddress;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Sets a virtual-host path that distinguishes this in-memory bus.</summary>
    /// <param name="virtualHost">The virtual-host path.</param>
    /// <param name="configure">An optional callback that configures the host.</param>
    public void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualHost);

        _hostConfiguration.BaseAddress = new UriBuilder(_hostConfiguration.HostAddress) { Path = virtualHost }.Uri;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>Gets the in-memory publish topology.</summary>
    public new IInMemoryPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Adds an in-memory receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the bus default.</param>
    /// <param name="configureEndpoint">An optional callback that augments in-memory endpoint configuration.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Adds a transport-independent receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the bus default.</param>
    /// <param name="configureEndpoint">An optional callback that augments endpoint configuration.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Adds an in-memory receive endpoint on a named queue.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configureEndpoint">The callback that configures the in-memory endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator> configureEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(configureEndpoint);
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Adds a transport-independent receive endpoint on a named queue.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configureEndpoint">The callback that configures the endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(configureEndpoint);
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }
}
