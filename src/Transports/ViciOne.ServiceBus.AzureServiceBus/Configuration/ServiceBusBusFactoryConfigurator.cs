using System;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service bus bus factory configurator implementation.
/// </summary>
public class ServiceBusBusFactoryConfigurator :
    BusFactoryConfigurator,
    IServiceBusBusFactoryConfigurator,
    IBusFactory
{
    readonly IServiceBusBusConfiguration _busConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ServiceBusQueueConfigurator _queueConfigurator;
    readonly ReceiveEndpointSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public ServiceBusBusFactoryConfigurator(IServiceBusBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        _queueConfigurator = new ServiceBusQueueConfigurator(queueName) { AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle };

        _settings = new ReceiveEndpointSettings(_busConfiguration.BusEndpointConfiguration, queueName, _queueConfigurator);
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
    /// Gets or sets the duplicate detection history time window value.
    /// </summary>
    public TimeSpan DuplicateDetectionHistoryTimeWindow
    {
        set => _queueConfigurator.DuplicateDetectionHistoryTimeWindow = value;
    }

    /// <summary>
    /// Gets or sets the enable partitioning value.
    /// </summary>
    public bool EnablePartitioning
    {
        set => _queueConfigurator.EnablePartitioning = value;
    }

    /// <summary>
    /// Gets or sets the max size in megabytes value.
    /// </summary>
    public long MaxSizeInMegabytes
    {
        set => _queueConfigurator.MaxSizeInMegabytes = value;
    }

    /// <summary>
    /// Gets or sets the max message size in kilobytes value.
    /// </summary>
    public long MaxMessageSizeInKilobytes
    {
        set => _settings.QueueConfigurator.MaxMessageSizeInKilobytes = value;
    }

    /// <summary>
    /// Gets or sets the requires duplicate detection value.
    /// </summary>
    public bool RequiresDuplicateDetection
    {
        set => _queueConfigurator.RequiresDuplicateDetection = value;
    }

    /// <summary>
    /// Gets or sets the max concurrent calls value.
    /// </summary>
    public int MaxConcurrentCalls
    {
        set => ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Performs the override default bus endpoint queue name operation.
    /// </summary>
    /// <param name="value">The value.</param>
    public void OverrideDefaultBusEndpointQueueName(string value)
    {
        _queueConfigurator.Path = value;
    }

    /// <summary>
    /// Sets namespace separator to tilde.
    /// </summary>
    public void SetNamespaceSeparatorToTilde()
    {
        _hostConfiguration.SetNamespaceSeparatorToTilde();
    }

    /// <summary>
    /// Sets namespace separator to underscore.
    /// </summary>
    public void SetNamespaceSeparatorToUnderscore()
    {
        _hostConfiguration.SetNamespaceSeparatorToUnderscore();
    }

    /// <summary>
    /// Sets namespace separator to.
    /// </summary>
    /// <param name="separator">The separator value.</param>
    public void SetNamespaceSeparatorTo(string separator)
    {
        _hostConfiguration.SetNamespaceSeparatorTo(separator);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Send<T>(Action<IServiceBusMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IServiceBusMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configureTopology">The configure topology value.</param>
    public void Publish<T>(Action<IServiceBusMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IServiceBusMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<IServiceBusMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public new IServiceBusSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new IServiceBusPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public void Host(ServiceBusHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IServiceBusReceiveEndpointConfigurator>? configureEndpoint)
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
    public void ReceiveEndpoint(string queueName, Action<IServiceBusReceiveEndpointConfigurator> configureEndpoint)
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
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator> configure)
        where T : class
    {
        _hostConfiguration.SubscriptionEndpoint<T>(subscriptionName, configure);
    }

    /// <summary>
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator> configure)
    {
        _hostConfiguration.SubscriptionEndpoint(subscriptionName, topicPath, configure);
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan AutoDeleteOnIdle
    {
        set => _queueConfigurator.AutoDeleteOnIdle = value;
    }

    /// <summary>
    /// Gets or sets the default message time to live value.
    /// </summary>
    public TimeSpan DefaultMessageTimeToLive
    {
        set => _queueConfigurator.DefaultMessageTimeToLive = value;
    }

    /// <summary>
    /// Gets or sets the enable batched operations value.
    /// </summary>
    public bool EnableBatchedOperations
    {
        set => _queueConfigurator.EnableBatchedOperations = value;
    }

    /// <summary>
    /// Gets or sets the enable dead lettering on message expiration value.
    /// </summary>
    public bool EnableDeadLetteringOnMessageExpiration
    {
        set => _queueConfigurator.EnableDeadLetteringOnMessageExpiration = value;
    }

    /// <summary>
    /// Gets or sets the forward dead lettered messages to value.
    /// </summary>
    public string ForwardDeadLetteredMessagesTo
    {
        set => _queueConfigurator.ForwardDeadLetteredMessagesTo = value;
    }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan LockDuration
    {
        set => _queueConfigurator.LockDuration = value;
    }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int MaxDeliveryCount
    {
        set => _queueConfigurator.MaxDeliveryCount = value;
    }

    /// <summary>
    /// Gets or sets the requires session value.
    /// </summary>
    public bool RequiresSession
    {
        set => _queueConfigurator.RequiresSession = value;
    }

    /// <summary>
    /// Gets or sets the max concurrent sessions value.
    /// </summary>
    public int MaxConcurrentSessions
    {
        set => _queueConfigurator.MaxConcurrentSessions = value;
    }

    /// <summary>
    /// Gets or sets the max concurrent calls per session value.
    /// </summary>
    public int MaxConcurrentCallsPerSession
    {
        set => _queueConfigurator.MaxConcurrentCallsPerSession = value;
    }

    /// <summary>
    /// Gets or sets the user metadata value.
    /// </summary>
    public string UserMetadata
    {
        set => _queueConfigurator.UserMetadata = value;
    }

    /// <summary>
    /// Gets or sets the session idle timeout value.
    /// </summary>
    public TimeSpan? SessionIdleTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>
    /// Gets or sets the max auto renew duration value.
    /// </summary>
    public TimeSpan MaxAutoRenewDuration
    {
        set => _settings.MaxAutoRenewDuration = value;
    }

    /// <summary>
    /// Performs the enable duplicate detection operation.
    /// </summary>
    /// <param name="historyTimeWindow">The history time window value.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        _queueConfigurator.RequiresDuplicateDetection = true;
        _queueConfigurator.DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }
}
