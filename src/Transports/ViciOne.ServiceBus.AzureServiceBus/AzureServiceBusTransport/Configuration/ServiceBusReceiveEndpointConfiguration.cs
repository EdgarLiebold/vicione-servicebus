using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Configures an Azure Service Bus queue receive endpoint and its topology.</summary>
public class ServiceBusReceiveEndpointConfiguration :
    ServiceBusEntityReceiveEndpointConfiguration,
    IServiceBusReceiveEndpointConfiguration,
    IServiceBusReceiveEndpointConfigurator
{
    readonly IServiceBusEndpointConfiguration _endpointConfiguration;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly ReceiveEndpointSettings _settings;

    /// <summary>Initializes a queue endpoint and applies the host's base path to its entity settings.</summary>
    /// <param name="hostConfiguration">The namespace host configuration.</param>
    /// <param name="settings">The queue and processor settings.</param>
    /// <param name="endpointConfiguration">The endpoint pipeline and topology configuration.</param>
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

    /// <summary>Gets the queue and processor settings.</summary>
    public ReceiveSettings Settings => _settings;

    /// <summary>Gets the namespace address.</summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;

    /// <summary>Gets the lazily formatted queue input address.</summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>Builds a receive-endpoint context from the current queue configuration.</summary>
    /// <returns>The Azure Service Bus receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateServiceBusReceiveEndpointContext();
    }

    IServiceBusTopologyConfiguration IServiceBusEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Combines queue-entity validation with the shared endpoint validation.</summary>
    /// <returns>All queue and endpoint validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return _settings.QueueConfigurator.Validate()
            .Concat(base.Validate());
    }

    /// <summary>Configures queue topology and error transports, then registers the receive endpoint with the host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
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

    /// <summary>Sets how long Azure Service Bus retains message identifiers for duplicate detection.</summary>
    public TimeSpan DuplicateDetectionHistoryTimeWindow
    {
        set => _settings.QueueConfigurator.DuplicateDetectionHistoryTimeWindow = value;
    }

    /// <summary>Enables duplicate detection and sets its identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long message identifiers remain available for duplicate detection.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        _settings.QueueConfigurator.RequiresDuplicateDetection = true;
        _settings.QueueConfigurator.DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }

    /// <summary>Sets whether the queue is partitioned.</summary>
    public bool EnablePartitioning
    {
        set => _settings.QueueConfigurator.EnablePartitioning = value;
    }

    /// <summary>Sets the maximum queue size in megabytes.</summary>
    public long MaxSizeInMegabytes
    {
        set => _settings.QueueConfigurator.MaxSizeInMegabytes = value;
    }

    /// <summary>Sets the maximum individual message size in kilobytes.</summary>
    public long MaxMessageSizeInKilobytes
    {
        set => _settings.QueueConfigurator.MaxMessageSizeInKilobytes = value;
    }

    /// <summary>Sets whether Azure Service Bus rejects duplicate message identifiers.</summary>
    public bool RequiresDuplicateDetection
    {
        set => _settings.QueueConfigurator.RequiresDuplicateDetection = value;
    }

    /// <summary>Sets whether topology cleanup removes subscriptions associated with this queue endpoint.</summary>
    public bool RemoveSubscriptions
    {
        set => _settings.RemoveSubscriptions = value;
    }

    /// <summary>Adds a consume-topology subscription from an explicit topic to this queue endpoint.</summary>
    /// <param name="topicName">The source topic name.</param>
    /// <param name="subscriptionName">The Azure Service Bus subscription name.</param>
    /// <param name="callback">An optional callback that configures the subscription.</param>
    public void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback)
    {
        _endpointConfiguration.Topology.Consume.Subscribe(topicName, subscriptionName, callback);
    }

    /// <summary>Adds a consume-topology subscription from a message type's topic to this queue endpoint.</summary>
    /// <typeparam name="T">The message type whose publish topology supplies the topic.</typeparam>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="callback">An optional callback that configures the subscription.</param>
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

    /// <summary>Determines whether the input address has been observed or base endpoint configuration has begun.</summary>
    /// <returns><see langword="true"/> when configuration can no longer be changed safely.</returns>
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
