using System;
using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Owns Azure Service Bus namespace settings, topology, endpoint definitions, and transport retry policies.</summary>
public class ServiceBusHostConfiguration :
    BaseHostConfiguration<IServiceBusEntityEndpointConfiguration, IServiceBusReceiveEndpointConfigurator>,
    IServiceBusHostConfiguration
{
    readonly IServiceBusBusConfiguration _busConfiguration;
    readonly IServiceBusBusTopology _busTopology;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IServiceBusTopologyConfiguration _topologyConfiguration;
    ServiceBusHostSettings _hostSettings;

    /// <summary>Initializes namespace configuration with transport retry policies and a recyclable connection supervisor.</summary>
    /// <param name="busConfiguration">The bus configuration that creates child endpoint configurations.</param>
    /// <param name="topologyConfiguration">The topology shared by hosted endpoints.</param>
    public ServiceBusHostConfiguration(IServiceBusBusConfiguration busConfiguration, IServiceBusTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _topologyConfiguration = topologyConfiguration;

        _hostSettings = new HostSettings();
        _busTopology = new ServiceBusBusTopology(this, _topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Ignore<UnauthorizedAccessException>();

            x.Handle<ConnectionException>();
            x.Handle<TimeoutException>();
            x.Handle<WebSocketException>();
            x.Handle<RequestFailedException>();
            x.Handle<ServiceBusException>(ex => ex.Reason switch
            {
                ServiceBusFailureReason.MessagingEntityDisabled => true,
                ServiceBusFailureReason.MessagingEntityNotFound => false,
                ServiceBusFailureReason.MessagingEntityAlreadyExists => false,
                ServiceBusFailureReason.MessageNotFound => false,
                ServiceBusFailureReason.MessageSizeExceeded => false,
                ServiceBusFailureReason.ServiceCommunicationProblem => true,
                ServiceBusFailureReason.ServiceBusy when ex.IsTransient => true,
                _ => false
            });

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        SendTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Ignore<UnauthorizedAccessException>();

            x.Handle<ConnectionException>();
            x.Handle<TimeoutException>();
            x.Handle<WebSocketException>();
            x.Handle<RequestFailedException>();
            x.Handle<ServiceBusException>(ex => ex.Reason switch
            {
                ServiceBusFailureReason.MessagingEntityNotFound => true,
                ServiceBusFailureReason.MessagingEntityAlreadyExists => true,
                ServiceBusFailureReason.MessageNotFound => false,
                ServiceBusFailureReason.MessageSizeExceeded => false,
                ServiceBusFailureReason.ServiceCommunicationProblem => true,
                ServiceBusFailureReason.ServiceBusy when ex.IsTransient => true,
                _ => false
            });

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() => new ConnectionContextSupervisor(this, topologyConfiguration));
    }

    /// <summary>Gets the configured Azure Service Bus namespace address.</summary>
    public override Uri HostAddress => _hostSettings.ServiceUri;

    string IServiceBusHostConfiguration.BasePath => _hostSettings.ServiceUri.AbsolutePath.Trim('/');

    /// <summary>Gets the recyclable supervisor for the shared namespace connection.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>Gets or sets the resolved namespace, credential, retry, and transport settings.</summary>
    public ServiceBusHostSettings Settings
    {
        get => _hostSettings;
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets the policy used to recover receive transports from transient namespace failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }
    /// <summary>Gets the policy used to recover send transports and recreate missing destinations.</summary>
    public override IRetryPolicy SendTransportRetryPolicy { get; }

    IServiceBusBusTopology IServiceBusHostConfiguration.Topology => _busTopology;

    /// <summary>Uses a tilde when formatting namespace segments in topic names.</summary>
    public void SetNamespaceSeparatorToTilde()
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter("~");
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>Uses an underscore when formatting namespace segments in topic names.</summary>
    public void SetNamespaceSeparatorToUnderscore()
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter("_");
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>Uses the specified separator when formatting namespace segments in topic names.</summary>
    /// <param name="separator">The separator inserted between namespace segments.</param>
    public void SetNamespaceSeparatorTo(string separator)
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter(separator);
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>Creates a queue endpoint from an endpoint definition and an optional provider-specific callback.</summary>
    /// <param name="definition">The definition that supplies the endpoint name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or the default formatter when omitted.</param>
    /// <param name="configureEndpoint">An optional callback that configures Azure Service Bus settings.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IServiceBusReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates and registers a receive-endpoint configuration for a queue.</summary>
    /// <param name="queueName">The queue name relative to the namespace.</param>
    /// <param name="configureEndpoint">The callback that configures the queue endpoint.</param>
    public override void ReceiveEndpoint(string queueName, Action<IServiceBusReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Applies common endpoint settings and temporary-entity behavior to an Azure Service Bus endpoint.</summary>
    /// <param name="configurator">The queue endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    public void ApplyEndpointDefinition(IServiceBusReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;
            configurator.RemoveSubscriptions = true;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers a queue endpoint using a fresh child endpoint configuration.</summary>
    /// <param name="queueName">The queue name relative to the namespace.</param>
    /// <param name="configure">An optional callback that configures the queue endpoint.</param>
    /// <returns>The registered queue endpoint configuration.</returns>
    public IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IServiceBusReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        var settings = new ReceiveEndpointSettings(endpointConfiguration, queueName, new ServiceBusQueueConfigurator(queueName));

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates, observes, and registers a queue endpoint from resolved settings.</summary>
    /// <param name="settings">The queue and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint pipeline and topology configuration.</param>
    /// <param name="configure">An optional callback that further configures the queue endpoint.</param>
    /// <returns>The registered queue endpoint configuration.</returns>
    public IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ReceiveEndpointSettings settings,
        IServiceBusEndpointConfiguration endpointConfiguration, Action<IServiceBusReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new ServiceBusReceiveEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }

    /// <summary>Creates and registers a subscription endpoint for a message type's publish topic.</summary>
    /// <typeparam name="T">The message type whose topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    public void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration,
            subscriptionName, _busConfiguration.Topology.Publish.GetMessageTopology<T>().CreateTopicOptions);

        CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates and registers a subscription endpoint for an explicit topic path.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The topic path.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    public void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration, subscriptionName, topicPath);

        CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Gets the Azure Service Bus topology built for this namespace.</summary>
    public override IBusTopology Topology => _busTopology;

    /// <summary>Creates a queue endpoint through the transport-independent host interface.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional transport-independent endpoint callback.</param>
    /// <returns>The registered queue endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure = null)
    {
        return CreateReceiveEndpointConfiguration(queueName,
            configure == null ? null : endpoint => configure(endpoint));
    }

    /// <summary>Builds the host and adds every registered queue and subscription endpoint.</summary>
    /// <returns>The configured Azure Service Bus host.</returns>
    public override IHost Build()
    {
        var host = new ServiceBusHost(this, _busTopology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }

    /// <summary>Creates a subscription endpoint for a message type's publish topic.</summary>
    /// <typeparam name="T">The message type whose topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>The registered subscription endpoint configuration.</returns>
    public IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration,
            subscriptionName, _busConfiguration.Topology.Publish.GetMessageTopology<T>().CreateTopicOptions);

        return CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates a subscription endpoint for an explicit topic path.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicPath">The topic path.</param>
    /// <param name="configure">An optional callback that configures the subscription endpoint.</param>
    /// <returns>The registered subscription endpoint configuration.</returns>
    public IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration(string subscriptionName, string topicPath,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration, subscriptionName, topicPath);

        return CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates, observes, and registers a subscription endpoint from resolved settings.</summary>
    /// <param name="settings">The topic, subscription, and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint pipeline and topology configuration.</param>
    /// <param name="configure">An optional callback that further configures the subscription endpoint.</param>
    /// <returns>The registered subscription endpoint configuration.</returns>
    public IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration(SubscriptionEndpointSettings settings,
        IServiceBusEndpointConfiguration endpointConfiguration, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new ServiceBusSubscriptionEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }
}
