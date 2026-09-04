using System;
using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus host configuration implementation.
/// </summary>
public class ServiceBusHostConfiguration :
    BaseHostConfiguration<IServiceBusEntityEndpointConfiguration, IServiceBusReceiveEndpointConfigurator>,
    IServiceBusHostConfiguration
{
    readonly IServiceBusBusConfiguration _busConfiguration;
    readonly IServiceBusBusTopology _busTopology;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IServiceBusTopologyConfiguration _topologyConfiguration;
    ServiceBusHostSettings _hostSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostSettings.ServiceUri;

    string IServiceBusHostConfiguration.BasePath => _hostSettings.ServiceUri.AbsolutePath.Trim('/');

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    public ServiceBusHostSettings Settings
    {
        get => _hostSettings;
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the receive transport retry policy value.
    /// </summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }
    /// <summary>
    /// Gets the send transport retry policy value.
    /// </summary>
    public override IRetryPolicy SendTransportRetryPolicy { get; }

    IServiceBusBusTopology IServiceBusHostConfiguration.Topology => _busTopology;

    /// <summary>
    /// Sets namespace separator to tilde.
    /// </summary>
    public void SetNamespaceSeparatorToTilde()
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter("~");
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>
    /// Sets namespace separator to underscore.
    /// </summary>
    public void SetNamespaceSeparatorToUnderscore()
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter("_");
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>
    /// Sets namespace separator to.
    /// </summary>
    /// <param name="separator">The separator value.</param>
    public void SetNamespaceSeparatorTo(string separator)
    {
        var messageNameFormatter = new ServiceBusMessageNameFormatter(separator);
        _topologyConfiguration.Message.SetEntityNameFormatter(new MessageNameFormatterEntityNameFormatter(messageNameFormatter));
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public override void ReceiveEndpoint(string queueName, Action<IServiceBusReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>
    /// Performs the apply endpoint definition operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="definition">The definition value.</param>
    public void ApplyEndpointDefinition(IServiceBusReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;
            configurator.RemoveSubscriptions = true;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IServiceBusReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IServiceBusReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        var settings = new ReceiveEndpointSettings(endpointConfiguration, queueName, new ServiceBusQueueConfigurator(queueName));

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void SubscriptionEndpoint<T>(string subscriptionName, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration,
            subscriptionName, _busConfiguration.Topology.Publish.GetMessageTopology<T>().CreateTopicOptions);

        CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Performs the subscription endpoint operation.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void SubscriptionEndpoint(string subscriptionName, string topicPath, Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration, subscriptionName, topicPath);

        CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public override IBusTopology Topology => _busTopology;

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure = null)
    {
        return CreateReceiveEndpointConfiguration(queueName,
            configure == null ? null : endpoint => configure(endpoint));
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IHost Build()
    {
        var host = new ServiceBusHost(this, _busTopology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }

    /// <summary>
    /// Creates subscription endpoint configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration<T>(string subscriptionName,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
        where T : class
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration,
            subscriptionName, _busConfiguration.Topology.Publish.GetMessageTopology<T>().CreateTopicOptions);

        return CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates subscription endpoint configuration.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IServiceBusSubscriptionEndpointConfiguration CreateSubscriptionEndpointConfiguration(string subscriptionName, string topicPath,
        Action<IServiceBusSubscriptionEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SubscriptionEndpointSettings(endpointConfiguration, subscriptionName, topicPath);

        return CreateSubscriptionEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates subscription endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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
