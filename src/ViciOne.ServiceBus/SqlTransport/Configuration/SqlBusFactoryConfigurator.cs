using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql bus factory configurator implementation.
/// </summary>
public class SqlBusFactoryConfigurator :
    BusFactoryConfigurator,
    ISqlBusFactoryConfigurator,
    IBusFactory
{
    readonly ISqlBusConfiguration _busConfiguration;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public SqlBusFactoryConfigurator(ISqlBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        _settings = new SqlReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, Defaults.TemporaryAutoDeleteOnIdle);
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
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set => _settings.AutoDeleteOnIdle = value;
    }

    /// <summary>
    /// Gets or sets the polling interval value.
    /// </summary>
    public TimeSpan PollingInterval
    {
        set => _settings.PollingInterval = value;
    }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan LockDuration
    {
        set => _settings.LockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max lock duration value.
    /// </summary>
    public TimeSpan MaxLockDuration
    {
        set => _settings.MaxLockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int? MaxDeliveryCount
    {
        set => _settings.MaxDeliveryCount = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Gets or sets the maintenance batch size value.
    /// </summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }

    /// <summary>
    /// Gets or sets the dead letter expired messages value.
    /// </summary>
    public bool DeadLetterExpiredMessages
    {
        set => _settings.DeadLetterExpiredMessages = value;
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public void Host(SqlHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Send<T>(Action<ISqlMessageSendTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        ISqlMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Publish<T>(Action<ISqlMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        ISqlMessagePublishTopologyConfigurator<T>? configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<ISqlMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public new ISqlSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new ISqlPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the override default bus endpoint queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    public void OverrideDefaultBusEndpointQueueName(string queueName)
    {
        _settings.QueueName = queueName;
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<ISqlReceiveEndpointConfigurator>? configureEndpoint)
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
    public void ReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator> configureEndpoint)
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
