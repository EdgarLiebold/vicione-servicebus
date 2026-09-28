using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Builds a RabbitMQ bus and delegates receive-endpoint creation to its host configuration.</summary>
public class RabbitMqBusFactoryConfigurator :
    BusFactoryConfigurator,
    IRabbitMqBusFactoryConfigurator,
    IBusFactory
{
    readonly IRabbitMqBusConfiguration _busConfiguration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>Creates a factory configurator with a temporary default bus endpoint.</summary>
    /// <param name="busConfiguration">The RabbitMQ bus configuration to update.</param>
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

    /// <summary>Creates the receive configuration for the temporary bus endpoint.</summary>
    /// <param name="configure">The provider-neutral endpoint configuration callback.</param>
    /// <returns>The configured bus receive endpoint.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(_settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Validates the provider-neutral bus settings and the RabbitMQ bus endpoint queue name.</summary>
    /// <returns>All bus-factory validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.QueueName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }

    /// <summary>Sets whether the bus endpoint queue survives broker restarts.</summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>Sets whether the bus endpoint queue is exclusive to this connection.</summary>
    public bool Exclusive
    {
        set => _settings.Exclusive = value;
    }

    /// <summary>Sets whether RabbitMQ deletes the bus endpoint topology when unused.</summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>Sets the bus endpoint exchange type.</summary>
    public string ExchangeType
    {
        set => _settings.ExchangeType = value;
    }

    /// <summary>Sets whether the bus endpoint queue is purged on first startup.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Sets the bus endpoint consumer priority.</summary>
    public int ConsumerPriority
    {
        set => _settings.ConsumerPriority = value;
    }

    /// <summary>Sets whether the broker permits only this consumer on the bus endpoint queue.</summary>
    public bool ExclusiveConsumer
    {
        set => _settings.ExclusiveConsumer = value;
    }

    /// <summary>Sets whether the bus endpoint uses RabbitMQ lazy-queue storage.</summary>
    public bool Lazy
    {
        set => _settings.Lazy = value;
    }

    /// <summary>Sets the unused-queue expiration for the bus endpoint in whole milliseconds.</summary>
    public TimeSpan? QueueExpiration
    {
        set => _settings.QueueExpiration = value;
    }

    /// <summary>Sets whether the bus endpoint uses RabbitMQ single-active-consumer semantics.</summary>
    public bool SingleActiveConsumer
    {
        set => _settings.SingleActiveConsumer = value;
    }

    /// <summary>Sets or removes a bus endpoint queue argument.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The argument value, or <see langword="null" /> to remove it.</param>
    public void SetQueueArgument(string key, object? value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>Sets a bus endpoint queue argument from a nonnegative duration converted to whole milliseconds; <c>x-expires</c> must be positive.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>Sets or removes a bus endpoint exchange argument.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The argument value, or <see langword="null" /> to remove it.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>Sets a bus endpoint exchange argument from a nonnegative duration converted to whole milliseconds.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>Enables priority delivery on the bus endpoint queue.</summary>
    /// <param name="maxPriority">The highest accepted message priority.</param>
    public void EnablePriority(byte maxPriority)
    {
        _settings.EnablePriority(maxPriority);
    }

    /// <summary>Configures the bus endpoint as a RabbitMQ quorum queue.</summary>
    /// <param name="replicationFactor">The optional initial quorum-group size.</param>
    public void SetQuorumQueue(int? replicationFactor = default)
    {
        _settings.SetQuorumQueue(replicationFactor);
    }

    /// <summary>Applies the host configuration.</summary>
    /// <param name="settings">The effective RabbitMQ connection settings.</param>
    public void Host(RabbitMqHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>Configures send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configureTopology">An optional callback that customizes message send topology.</param>
    public void Send<T>(Action<IRabbitMqMessageSendTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IRabbitMqMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configureTopology">An optional callback that customizes message publish topology.</param>
    public void Publish<T>(Action<IRabbitMqMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IRabbitMqMessagePublishTopologyConfigurator<T>? configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures publish topology for a runtime message-contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that customizes message publish topology.</param>
    public void Publish(Type messageType, Action<IRabbitMqMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Gets the send topology.</summary>
    public new IRabbitMqSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the publish topology.</summary>
    public new IRabbitMqPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Overrides the exchange and queue name of the bus endpoint.</summary>
    /// <param name="queueName">The replacement bus-endpoint entity name.</param>
    public void OverrideDefaultBusEndpointQueueName(string queueName)
    {
        _settings.ExchangeName = queueName;
        _settings.QueueName = queueName;
    }

    /// <summary>Creates a RabbitMQ receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">An optional RabbitMQ endpoint callback.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Creates a RabbitMQ receive endpoint from an endpoint definition and provider-neutral callback.</summary>
    /// <param name="definition">The endpoint definition that supplies queue and concurrency settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or the default formatter when omitted.</param>
    /// <param name="configureEndpoint">An optional provider-neutral endpoint callback.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Creates a RabbitMQ receive endpoint for an explicit queue name.</summary>
    /// <param name="queueName">The receive queue name.</param>
    /// <param name="configureEndpoint">The RabbitMQ endpoint callback.</param>
    public void ReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Creates a RabbitMQ receive endpoint for an explicit queue name.</summary>
    /// <param name="queueName">The receive queue name.</param>
    /// <param name="configureEndpoint">The provider-neutral endpoint callback.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }
}
