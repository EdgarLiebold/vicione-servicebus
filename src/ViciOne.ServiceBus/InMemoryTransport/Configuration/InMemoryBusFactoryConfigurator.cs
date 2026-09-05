using System;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Provides an in memory bus factory configurator implementation.
/// </summary>
public class InMemoryBusFactoryConfigurator :
    BusFactoryConfigurator,
    IInMemoryBusFactoryConfigurator,
    IBusFactory
{
    readonly IInMemoryBusConfiguration _busConfiguration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public InMemoryBusFactoryConfigurator(IInMemoryBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        busConfiguration.BusEndpointConfiguration.Consume.Configurator.AutoStart = true;
    }

    /// <summary>
    /// Creates bus endpoint configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        return _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Publish<T>(Action<IInMemoryMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IInMemoryMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<IInMemoryMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Host(Action<IInMemoryHostConfigurator>? configure)
    {
        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Host(Uri baseAddress, Action<IInMemoryHostConfigurator>? configure)
    {
        _hostConfiguration.BaseAddress = baseAddress;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="virtualHost">The virtual host value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Host(string virtualHost, Action<IInMemoryHostConfigurator>? configure)
    {
        _hostConfiguration.BaseAddress = new UriBuilder(_hostConfiguration.HostAddress) { Path = virtualHost }.Uri;

        configure?.Invoke(_hostConfiguration.Configurator);
    }

    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new IInMemoryPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }
}
