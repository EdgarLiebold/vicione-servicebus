using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs bus factory configurator implementation.
/// </summary>
public class AmazonSqsBusFactoryConfigurator :
    BusFactoryConfigurator,
    IAmazonSqsBusFactoryConfigurator,
    IBusFactory
{
    readonly IAmazonSqsBusConfiguration _busConfiguration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly QueueReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public AmazonSqsBusFactoryConfigurator(IAmazonSqsBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        _settings = new QueueReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, false, true);
    }

    /// <summary>
    /// Gets or sets the wait time seconds value.
    /// </summary>
    public ushort WaitTimeSeconds
    {
        set => _settings.WaitTimeSeconds = AmazonSqsReceiveSettingsLimits.WaitTimeSeconds(value);
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Performs the override default bus endpoint queue name operation.
    /// </summary>
    /// <param name="value">The value.</param>
    public void OverrideDefaultBusEndpointQueueName(string value)
    {
        _settings.EntityName = value;
    }

    /// <summary>
    /// Gets the queue attributes value.
    /// </summary>
    public IDictionary<string, object> QueueAttributes => _settings.QueueAttributes;
    /// <summary>
    /// Gets the queue subscription attributes value.
    /// </summary>
    public IDictionary<string, object> QueueSubscriptionAttributes => _settings.QueueSubscriptionAttributes;
    /// <summary>
    /// Gets the queue tags value.
    /// </summary>
    public IDictionary<string, string> QueueTags => _settings.QueueTags;

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
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

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<IAmazonSqsMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public new IAmazonSqsSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new IAmazonSqsPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint)
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
    public void ReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator> configureEndpoint)
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

    /// <summary>
    /// Creates bus endpoint configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator>? configure)
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

        if (string.IsNullOrWhiteSpace(_settings.EntityName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }
}
