using System;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds an Azure Service Bus bus and its temporary control endpoint.</summary>
public class ServiceBusBusFactoryConfigurator :
    BusFactoryConfigurator,
    IServiceBusBusFactoryConfigurator,
    IBusFactory
{
    readonly IServiceBusBusConfiguration _busConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ServiceBusQueueConfigurator _queueConfigurator;
    readonly ReceiveEndpointSettings _settings;

    /// <summary>Creates a configurator for the supplied Azure Service Bus configuration.</summary>
    /// <param name="busConfiguration">The bus, host, and topology configuration to apply.</param>
    public ServiceBusBusFactoryConfigurator(IServiceBusBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        _queueConfigurator = new ServiceBusQueueConfigurator(queueName) { AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle };

        _settings = new ReceiveEndpointSettings(_busConfiguration.BusEndpointConfiguration, queueName, _queueConfigurator);
    }

    /// <summary>Creates the temporary receive endpoint used by the bus instance.</summary>
    /// <param name="configure">Configures the temporary bus endpoint.</param>
    /// <returns>The completed endpoint configuration.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(_settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Sets how long the temporary bus queue retains message identifiers for duplicate detection.</summary>
    public TimeSpan DuplicateDetectionHistoryTimeWindow
    {
        set => _queueConfigurator.DuplicateDetectionHistoryTimeWindow = value;
    }

    /// <summary>Enables or disables partitioning on the temporary bus queue.</summary>
    public bool EnablePartitioning
    {
        set => _queueConfigurator.EnablePartitioning = value;
    }

    /// <summary>Sets the maximum size of the temporary bus queue, in megabytes.</summary>
    public long MaxSizeInMegabytes
    {
        set => _queueConfigurator.MaxSizeInMegabytes = value;
    }

    /// <summary>Sets the maximum size of an individual message on the temporary bus queue, in kilobytes.</summary>
    public long MaxMessageSizeInKilobytes
    {
        set => _settings.QueueConfigurator.MaxMessageSizeInKilobytes = value;
    }

    /// <summary>Enables or disables duplicate detection on the temporary bus queue.</summary>
    public bool RequiresDuplicateDetection
    {
        set => _queueConfigurator.RequiresDuplicateDetection = value;
    }

    /// <summary>Sets the maximum number of concurrent message callbacks for the bus endpoint.</summary>
    public int MaxConcurrentCalls
    {
        set => ConcurrentMessageLimit = value;
    }

    /// <summary>Overrides the generated name of the temporary bus endpoint queue.</summary>
    /// <param name="value">The namespace-relative queue name.</param>
    public void OverrideDefaultBusEndpointQueueName(string value)
    {
        _queueConfigurator.Path = value;
    }

    /// <summary>Uses a tilde as the namespace separator in generated entity names.</summary>
    public void SetNamespaceSeparatorToTilde()
    {
        _hostConfiguration.SetNamespaceSeparatorToTilde();
    }

    /// <summary>Uses an underscore as the namespace separator in generated entity names.</summary>
    public void SetNamespaceSeparatorToUnderscore()
    {
        _hostConfiguration.SetNamespaceSeparatorToUnderscore();
    }

    /// <summary>Sets the namespace separator used in generated entity names.</summary>
    /// <param name="separator">The separator string.</param>
    public void SetNamespaceSeparatorTo(string separator)
    {
        _hostConfiguration.SetNamespaceSeparatorTo(separator);
    }

    /// <summary>Configures send topology for a message type.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">Configures the message-specific send topology.</param>
    public void Send<T>(Action<IServiceBusMessageSendTopologyConfigurator<T>> configureTopology)
        where T : class
    {
        IServiceBusMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures publish topology for a message type.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configureTopology">Optionally configures the message-specific publish topology.</param>
    public void Publish<T>(Action<IServiceBusMessagePublishTopologyConfigurator<T>>? configureTopology)
        where T : class
    {
        IServiceBusMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Configures publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">Optionally configures the message-specific publish topology.</param>
    public void Publish(Type messageType, Action<IServiceBusMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Gets the bus-wide Azure Service Bus send topology.</summary>
    public new IServiceBusSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the bus-wide Azure Service Bus publish topology.</summary>
    public new IServiceBusPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Replaces the Azure Service Bus namespace settings.</summary>
    /// <param name="settings">The namespace connection and retry settings.</param>
    public void Host(ServiceBusHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = settings;
    }

    /// <summary>Registers a receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The optional formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">Optionally configures Azure Service Bus-specific endpoint settings.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IServiceBusReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Registers a receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The optional formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">Optionally configures provider-neutral endpoint settings.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Registers a receive endpoint for a named Azure Service Bus queue.</summary>
    /// <param name="queueName">The namespace-relative queue name.</param>
    /// <param name="configureEndpoint">Configures Azure Service Bus-specific endpoint settings.</param>
    public void ReceiveEndpoint(string queueName, Action<IServiceBusReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Registers a receive endpoint for a named Azure Service Bus queue.</summary>
    /// <param name="queueName">The namespace-relative queue name.</param>
    /// <param name="configureEndpoint">Configures provider-neutral endpoint settings.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Configures the subscription endpoint.</summary>
    /// <typeparam name="T">The message contract whose publish topic is subscribed.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Configures the subscription endpoint.</param>
    public void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator> configure)
        where T : class
    {
        _hostConfiguration.SubscriptionEndpoint<T>(subscriptionName, configure);
    }

    /// <summary>Configures the subscription endpoint.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The namespace-relative topic path.</param>
    /// <param name="configure">Configures the subscription endpoint.</param>
    public void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator> configure)
    {
        _hostConfiguration.SubscriptionEndpoint(subscriptionName, topicPath, configure);
    }

    /// <summary>Sets the idle duration after which the temporary bus queue is deleted.</summary>
    public TimeSpan AutoDeleteOnIdle
    {
        set => _queueConfigurator.AutoDeleteOnIdle = value;
    }

    /// <summary>Sets the default time to live for messages on the temporary bus queue.</summary>
    public TimeSpan DefaultMessageTimeToLive
    {
        set => _queueConfigurator.DefaultMessageTimeToLive = value;
    }

    /// <summary>Enables or disables broker-side batching on the temporary bus queue.</summary>
    public bool EnableBatchedOperations
    {
        set => _queueConfigurator.EnableBatchedOperations = value;
    }

    /// <summary>Enables or disables dead-lettering of expired messages.</summary>
    public bool EnableDeadLetteringOnMessageExpiration
    {
        set => _queueConfigurator.EnableDeadLetteringOnMessageExpiration = value;
    }

    /// <summary>Sets the entity path to which dead-lettered messages are forwarded.</summary>
    public string ForwardDeadLetteredMessagesTo
    {
        set => _queueConfigurator.ForwardDeadLetteredMessagesTo = value;
    }

    /// <summary>Sets the initial lock duration for received messages.</summary>
    public TimeSpan LockDuration
    {
        set => _queueConfigurator.LockDuration = value;
    }

    /// <summary>Sets the delivery-attempt limit before a message is dead-lettered.</summary>
    public int MaxDeliveryCount
    {
        set => _queueConfigurator.MaxDeliveryCount = value;
    }

    /// <summary>Sets whether the temporary bus queue requires sessions.</summary>
    public bool RequiresSession
    {
        set => _queueConfigurator.RequiresSession = value;
    }

    /// <summary>Sets the maximum number of sessions processed concurrently.</summary>
    public int MaxConcurrentSessions
    {
        set => _queueConfigurator.MaxConcurrentSessions = value;
    }

    /// <summary>Sets the maximum number of concurrent message callbacks for each session.</summary>
    public int MaxConcurrentCallsPerSession
    {
        set => _queueConfigurator.MaxConcurrentCallsPerSession = value;
    }

    /// <summary>Sets provider metadata stored with the temporary bus queue.</summary>
    public string UserMetadata
    {
        set => _queueConfigurator.UserMetadata = value;
    }

    /// <summary>Sets how long the processor waits for another message in the active session.</summary>
    public TimeSpan? SessionIdleTimeout
    {
        set => _settings.SessionIdleTimeout = value;
    }

    /// <summary>Sets how long the processor automatically renews message or session locks.</summary>
    public TimeSpan MaxAutoRenewDuration
    {
        set => _settings.MaxAutoRenewDuration = value;
    }

    /// <summary>Enables duplicate detection and sets the identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long message identifiers remain available for duplicate detection.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        _queueConfigurator.RequiresDuplicateDetection = true;
        _queueConfigurator.DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }
}
