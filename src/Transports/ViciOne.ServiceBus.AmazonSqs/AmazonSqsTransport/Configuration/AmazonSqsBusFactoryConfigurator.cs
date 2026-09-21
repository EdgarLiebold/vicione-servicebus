using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Configures an Amazon SQS bus and its default temporary bus endpoint.</summary>
public class AmazonSqsBusFactoryConfigurator :
    BusFactoryConfigurator,
    IAmazonSqsBusFactoryConfigurator,
    IBusFactory
{
    readonly IAmazonSqsBusConfiguration _busConfiguration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly QueueReceiveSettings _settings;

    /// <summary>Initializes an Amazon SQS bus-factory configurator.</summary>
    /// <param name="busConfiguration">The bus configuration to update.</param>
    public AmazonSqsBusFactoryConfigurator(IAmazonSqsBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        _settings = new QueueReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, false, true);
    }

    /// <summary>Sets the default Amazon SQS long-poll wait time, in seconds.</summary>
    public ushort WaitTimeSeconds
    {
        set => _settings.WaitTimeSeconds = AmazonSqsReceiveSettingsLimits.WaitTimeSeconds(value);
    }

    /// <summary>Sets whether the default bus queue is retained when the bus stops.</summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>Sets whether the default bus queue is deleted when the bus stops.</summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>Sets whether available messages are purged from the default bus queue during startup.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Overrides the generated name of the default bus endpoint queue.</summary>
    /// <param name="value">The queue name to use.</param>
    public void OverrideDefaultBusEndpointQueueName(string value)
    {
        _settings.EntityName = value;
    }

    /// <summary>Gets the Amazon SQS attributes applied to the default bus queue.</summary>
    public IDictionary<string, object> QueueAttributes => _settings.QueueAttributes;
    /// <summary>Gets the Amazon SNS subscription attributes applied to subscriptions targeting the default bus queue.</summary>
    public IDictionary<string, object> QueueSubscriptionAttributes => _settings.QueueSubscriptionAttributes;
    /// <summary>Gets the tags applied to the default bus queue.</summary>
    public IDictionary<string, string> QueueTags => _settings.QueueTags;

    /// <summary>Applies frozen Amazon SQS host settings to the bus.</summary>
    /// <param name="settings">The host settings to use.</param>
    public void Host(AmazonSqsHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    void IAmazonSqsBusFactoryConfigurator.Send<T>(Action<IAmazonSqsMessageSendTopologyConfigurator<T>> configureTopology)
    {
        IAmazonSqsMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    void IAmazonSqsBusFactoryConfigurator.Publish<T>(Action<IAmazonSqsMessagePublishTopologyConfigurator<T>>? configureTopology)
    {
        IAmazonSqsMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures Amazon SNS publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures the message publish topology.</param>
    public void Publish(Type messageType, Action<IAmazonSqsMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Gets the Amazon SQS send-topology configurator.</summary>
    public new IAmazonSqsSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the Amazon SNS publish-topology configurator.</summary>
    public new IAmazonSqsPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Configures an Amazon SQS receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the configured default.</param>
    /// <param name="configureEndpoint">An optional Amazon SQS-specific endpoint callback.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Configures a receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the configured default.</param>
    /// <param name="configureEndpoint">An optional transport-neutral endpoint callback.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Configures an Amazon SQS receive endpoint for a named queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The Amazon SQS-specific endpoint callback.</param>
    public void ReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Configures a receive endpoint for a named Amazon SQS queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The transport-neutral endpoint callback.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Creates the receive-endpoint configuration for the default bus queue.</summary>
    /// <param name="configure">An optional transport-neutral endpoint callback.</param>
    /// <returns>The configured default bus endpoint.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator>? configure)
    {
        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(_settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Validates the bus and requires a nonempty default bus queue name.</summary>
    /// <returns>All detected validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.EntityName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");

        if (!HasMessageLimits)
            yield return this.Failure("Bus", "Message limits for this Amazon SQS bus are missing. Call configurator.Limits(...) with explicit body and envelope limits before building the bus");
    }
}
