using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures sql bus factory.</summary>
public class SqlBusFactoryConfigurator :
    BusFactoryConfigurator,
    ISqlBusFactoryConfigurator,
    IBusFactory
{
    readonly ISqlBusConfiguration _busConfiguration;
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly SqlReceiveSettings _settings;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busConfiguration">The bus configuration.</param>
    public SqlBusFactoryConfigurator(ISqlBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        _settings = new SqlReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName,
            SqlTransportDefaults.TemporaryQueueAutoDeleteOnIdle);
    }

    /// <summary>Creates bus endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created bus endpoint configuration.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(_settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.QueueName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }

    /// <summary>Gets or sets the auto delete on idle.</summary>
    public TimeSpan? AutoDeleteOnIdle
    {
        set => _settings.AutoDeleteOnIdle = value;
    }

    /// <summary>Gets or sets the polling interval.</summary>
    public TimeSpan PollingInterval
    {
        set => _settings.PollingInterval = value;
    }

    /// <summary>Gets or sets the lock duration.</summary>
    public TimeSpan LockDuration
    {
        set => _settings.LockDuration = value;
    }

    /// <summary>Gets or sets the max lock duration.</summary>
    public TimeSpan MaxLockDuration
    {
        set => _settings.MaxLockDuration = value;
    }

    /// <summary>Gets or sets the max delivery count.</summary>
    public int? MaxDeliveryCount
    {
        set => _settings.MaxDeliveryCount = value;
    }

    /// <summary>Gets or sets the purge on startup.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Gets or sets the maintenance batch size.</summary>
    public int MaintenanceBatchSize
    {
        set => _settings.MaintenanceBatchSize = value;
    }

    /// <summary>Gets or sets the dead letter expired messages.</summary>
    public bool DeadLetterExpiredMessages
    {
        set => _settings.DeadLetterExpiredMessages = value;
    }

    /// <summary>Applies the host configuration.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public void Host(SqlHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    public void Send<T>(Action<ISqlMessageSendTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        ISqlMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configureTopology">The configure topology.</param>
    public void Publish<T>(Action<ISqlMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        ISqlMessagePublishTopologyConfigurator<T>? configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Publish(Type messageType, Action<ISqlMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Gets the send topology.</summary>
    public new ISqlSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the publish topology.</summary>
    public new ISqlPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Overrides default bus endpoint queue name.</summary>
    /// <param name="queueName">The queue name.</param>
    public void OverrideDefaultBusEndpointQueueName(string queueName)
    {
        _settings.QueueName = queueName;
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<ISqlReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator> configureEndpoint)
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
