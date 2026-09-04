using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq bus factory configurator implementation.
/// </summary>
public class RabbitMqBusFactoryConfigurator :
    BusFactoryConfigurator,
    IRabbitMqBusFactoryConfigurator,
    IBusFactory
{
    readonly IRabbitMqBusConfiguration _busConfiguration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public RabbitMqBusFactoryConfigurator(IRabbitMqBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        var exchangeType = busConfiguration.BusEndpointConfiguration.Topology.Consume.ExchangeTypeSelector.DefaultExchangeType;
        _settings = new RabbitMqReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, exchangeType, false, true)
        {
            QueueExpiration = TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Creates bus endpoint configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(_settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.QueueName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>
    /// Gets or sets the exclusive value.
    /// </summary>
    public bool Exclusive
    {
        set => _settings.Exclusive = value;
    }

    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    public string ExchangeType
    {
        set => _settings.ExchangeType = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Gets or sets the consumer priority value.
    /// </summary>
    public int ConsumerPriority
    {
        set => _settings.ConsumerPriority = value;
    }

    /// <summary>
    /// Gets or sets the exclusive consumer value.
    /// </summary>
    public bool ExclusiveConsumer
    {
        set => _settings.ExclusiveConsumer = value;
    }

    /// <summary>
    /// Gets or sets the lazy value.
    /// </summary>
    public bool Lazy
    {
        set => _settings.Lazy = value;
    }

    /// <summary>
    /// Gets or sets the queue expiration value.
    /// </summary>
    public TimeSpan? QueueExpiration
    {
        set => _settings.QueueExpiration = value;
    }

    /// <summary>
    /// Gets or sets the single active consumer value.
    /// </summary>
    public bool SingleActiveConsumer
    {
        set => _settings.SingleActiveConsumer = value;
    }

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, object? value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>
    /// Performs the enable priority operation.
    /// </summary>
    /// <param name="maxPriority">The max priority value.</param>
    public void EnablePriority(byte maxPriority)
    {
        _settings.EnablePriority(maxPriority);
    }

    /// <summary>
    /// Sets quorum queue.
    /// </summary>
    /// <param name="replicationFactor">The replication factor value.</param>
    public void SetQuorumQueue(int? replicationFactor = default)
    {
        _settings.SetQuorumQueue(replicationFactor);
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public void Host(RabbitMqHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Send<T>(Action<IRabbitMqMessageSendTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IRabbitMqMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Publish<T>(Action<IRabbitMqMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IRabbitMqMessagePublishTopologyConfigurator<T>? configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<IRabbitMqMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public new IRabbitMqSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new IRabbitMqPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the override default bus endpoint queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    public void OverrideDefaultBusEndpointQueueName(string queueName)
    {
        _settings.ExchangeName = queueName;
        _settings.QueueName = queueName;
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint)
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
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator> configureEndpoint)
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
