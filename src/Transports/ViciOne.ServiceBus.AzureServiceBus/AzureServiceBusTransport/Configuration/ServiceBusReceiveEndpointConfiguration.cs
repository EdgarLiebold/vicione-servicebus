using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus receive endpoint configuration implementation.
/// </summary>
public class ServiceBusReceiveEndpointConfiguration :
    ServiceBusEntityReceiveEndpointConfiguration,
    IServiceBusReceiveEndpointConfiguration,
    IServiceBusReceiveEndpointConfigurator
{
    readonly IServiceBusEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly ReceiveEndpointSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public ServiceBusReceiveEndpointConfiguration(IServiceBusHostConfiguration hostConfiguration, ReceiveEndpointSettings settings,
        IServiceBusEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, settings, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _endpointConfiguration = endpointConfiguration;
        _settings = settings;

        _settings.QueueConfigurator.BasePath = hostConfiguration.BasePath;

        _inputAddress = new Lazy<Uri>(FormatInputAddress);
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public ReceiveSettings Settings => _settings;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateServiceBusReceiveEndpointContext();
    }

    IServiceBusTopologyConfiguration IServiceBusEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return _settings.QueueConfigurator.Validate()
            .Concat(base.Validate());
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    public void Build(IHost host)
    {
        var context = CreateServiceBusReceiveEndpointContext();

        ClientPipeConfigurator.UseFilter(new ConfigureServiceBusTopologyFilter<ReceiveSettings>(_settings, context.BrokerTopology,
            _settings.RemoveSubscriptions, context));

        var errorTransport = CreateErrorTransport();
        var deadLetterTransport = CreateDeadLetterTransport();

        context.GetOrAddPayload(() => deadLetterTransport);
        context.GetOrAddPayload(() => errorTransport);

        CreateReceiveEndpoint(host, context);
    }

    /// <summary>
    /// Gets or sets the duplicate detection history time window value.
    /// </summary>
    public TimeSpan DuplicateDetectionHistoryTimeWindow
    {
        set => _settings.QueueConfigurator.DuplicateDetectionHistoryTimeWindow = value;
    }

    /// <summary>
    /// Performs the enable duplicate detection operation.
    /// </summary>
    /// <param name="historyTimeWindow">The history time window value.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        _settings.QueueConfigurator.RequiresDuplicateDetection = true;
        _settings.QueueConfigurator.DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }

    /// <summary>
    /// Gets or sets the enable partitioning value.
    /// </summary>
    public bool EnablePartitioning
    {
        set => _settings.QueueConfigurator.EnablePartitioning = value;
    }

    /// <summary>
    /// Gets or sets the max size in megabytes value.
    /// </summary>
    public long MaxSizeInMegabytes
    {
        set => _settings.QueueConfigurator.MaxSizeInMegabytes = value;
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
        set => _settings.QueueConfigurator.RequiresDuplicateDetection = value;
    }

    /// <summary>
    /// Gets or sets the remove subscriptions value.
    /// </summary>
    public bool RemoveSubscriptions
    {
        set => _settings.RemoveSubscriptions = value;
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="callback">The callback value.</param>
    public void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback)
    {
        _endpointConfiguration.Topology.Consume.Subscribe(topicName, subscriptionName, callback);
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="callback">The callback value.</param>
    public void Subscribe<T>(string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Subscribe(subscriptionName, callback);
    }

    ServiceBusReceiveEndpointContext CreateServiceBusReceiveEndpointContext()
    {
        var builder = new ServiceBusReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress, _settings.Path);
    }

    /// <summary>
    /// Determines whether already configured.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }

    IErrorTransport CreateErrorTransport()
    {
        var settings = _endpointConfiguration.Topology.Send.GetErrorSettings(_settings.QueueConfigurator);

        return new ServiceBusQueueErrorTransport(_hostConfiguration.ConnectionContextSupervisor, settings);
    }

    IDeadLetterTransport CreateDeadLetterTransport()
    {
        var settings = _endpointConfiguration.Topology.Send.GetDeadLetterSettings(_settings.QueueConfigurator);

        return new ServiceBusQueueDeadLetterTransport(_hostConfiguration.ConnectionContextSupervisor, settings);
    }
}
