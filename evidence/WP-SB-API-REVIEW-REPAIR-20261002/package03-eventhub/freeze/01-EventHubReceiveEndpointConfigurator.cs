using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EventHubs.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Builds an Event Hubs processor-backed receive endpoint from bus, namespace, and checkpoint settings.</summary>
public class EventHubReceiveEndpointConfigurator :
    ReceiveEndpointConfiguration,
    IEventHubReceiveEndpointConfigurator,
    ReceiveSettings
{
    readonly Lazy<BlobContainerClient> _blobClient;
    readonly IBusInstance _busInstance;
    readonly IReceiveEndpointConfiguration _endpointConfiguration;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly IHostSettings _hostSettings;
    readonly PipeConfigurator<ProcessorContext> _processorConfigurator;
    readonly IStorageSettings _storageSettings;
    Action<EventProcessorClientOptions>? _configureOptions;
    string? _containerName;
    Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;

    /// <summary>Creates an endpoint configurator with conservative concurrency and batched-checkpoint defaults.</summary>
    /// <param name="hostConfiguration">The Event Hubs rider host configuration.</param>
    /// <param name="busInstance">The bus instance that will own the endpoint.</param>
    /// <param name="endpointConfiguration">The receive endpoint's pipe configuration.</param>
    /// <param name="hostSettings">The Event Hubs namespace connection settings.</param>
    /// <param name="storageSettings">The Blob Storage checkpoint settings.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    public EventHubReceiveEndpointConfigurator(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance,
        IReceiveEndpointConfiguration endpointConfiguration, IHostSettings hostSettings, IStorageSettings storageSettings, string eventHubName,
        string consumerGroup)
        : base(busInstance.HostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _endpointConfiguration = endpointConfiguration;
        _hostSettings = hostSettings;
        _storageSettings = storageSettings;

        EventHubName = eventHubName;
        ConsumerGroup = consumerGroup;

        ConcurrentMessageLimit = 1;
        ConcurrentDeliveryLimit = 1;

        CheckpointInterval = TimeSpan.FromMinutes(1);
        CheckpointMessageCount = 5000;
        CheckpointMessageLimit = 10000;

        PrefetchCount = Math.Max(1000, CheckpointMessageCount / 10);

        _processorConfigurator = new PipeConfigurator<ProcessorContext>();
        _blobClient = new Lazy<BlobContainerClient>(CreateBlobClient);

        PublishFaults = false;

        this.DiscardFaultedMessages();
        this.DiscardSkippedMessages();
    }

    /// <summary>Validates the Event Hubs receive settings before endpoint construction.</summary>
    /// <returns>Provider and inherited endpoint validation failures.</returns>
    public override System.Collections.Generic.IEnumerable<ValidationResult> Validate()
    {
        if (CheckpointMessageCount == 0)
            yield return this.Failure(nameof(CheckpointMessageCount),
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    $"Event Hubs receive endpoint '{EventHubName}/{ConsumerGroup}'", _busInstance.Name,
                    "CheckpointMessageCount is outside its valid range", "Set CheckpointMessageCount to a positive completed-event count"));

        if (CheckpointMessageLimit == 0)
            yield return this.Failure(nameof(CheckpointMessageLimit),
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    $"Event Hubs receive endpoint '{EventHubName}/{ConsumerGroup}'", _busInstance.Name,
                    "CheckpointMessageLimit is outside its valid range", "Set CheckpointMessageLimit to a positive retained-event limit"));

        if (CheckpointInterval <= TimeSpan.Zero || (long)CheckpointInterval.TotalMilliseconds > uint.MaxValue - 1L)
            yield return this.Failure(nameof(CheckpointInterval),
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    $"Event Hubs receive endpoint '{EventHubName}/{ConsumerGroup}'", _busInstance.Name,
                    "CheckpointInterval is outside its valid range", "Set CheckpointInterval to a positive interval whose whole milliseconds are no greater than 4294967294"));

        if (ConcurrentDeliveryLimit <= 0)
            yield return this.Failure(nameof(ConcurrentDeliveryLimit),
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    $"Event Hubs receive endpoint '{EventHubName}/{ConsumerGroup}'", _busInstance.Name,
                    "ConcurrentDeliveryLimit is outside its valid range", "Set ConcurrentDeliveryLimit to a positive delivery limit"));

        if (PrefetchCount == 0)
            yield return this.Failure(nameof(PrefetchCount),
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    $"Event Hubs receive endpoint '{EventHubName}/{ConsumerGroup}'", _busInstance.Name,
                    "PrefetchCount is outside its valid range", "Set PrefetchCount to a positive receive admission limit"));

        foreach (var result in base.Validate())
            yield return result;
    }

    /// <summary>Gets the bus host address exposed by the endpoint configuration.</summary>
    public override Uri HostAddress => _endpointConfiguration.HostAddress;

    /// <summary>Gets or sets the Blob container name, defaulting to the Event Hub entity name.</summary>
    public string ContainerName
    {
        get => _containerName ?? EventHubName;
        set
        {
            _containerName = value ?? throw new ArgumentNullException(nameof(value));

            Changed(nameof(ContainerName));
        }
    }

    /// <summary>Gets or sets the maximum time between completed partition checkpoints.</summary>
    public TimeSpan CheckpointInterval { get; set; }
    /// <summary>Gets or sets the maximum number of uncheckpointed events retained for one partition.</summary>
    public ushort CheckpointMessageLimit { get; set; }
    /// <summary>Gets or sets the number of completed events that triggers a partition checkpoint.</summary>
    public ushort CheckpointMessageCount { get; set; }

    /// <summary>Sets the callback applied when the event processor client options are created.</summary>
    public Action<EventProcessorClientOptions> ConfigureOptions
    {
        set => _configureOptions = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Registers the single application handler invoked after processing stops for a partition.</summary>
    /// <param name="handler">The asynchronous partition-closing handler.</param>
    public void OnPartitionClosing(Func<PartitionClosingEventArgs, Task> handler)
    {
        if (_partitionClosingHandler != null)
            throw new InvalidOperationException("Partition closing event handler may not be specified more than once.");
        _partitionClosingHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>Registers the single application handler invoked before processing begins for a partition.</summary>
    /// <param name="handler">The asynchronous partition-initializing handler.</param>
    public void OnPartitionInitializing(Func<PartitionInitializingEventArgs, Task> handler)
    {
        if (_partitionInitializingHandler != null)
            throw new InvalidOperationException("Partition initializing event handler may not be specified more than once.");
        _partitionInitializingHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>Gets the bus input address for this Event Hub and consumer group.</summary>
    public override Uri InputAddress => _endpointConfiguration.InputAddress;
    /// <summary>Gets or sets concurrent deliveries permitted for events with the same partition key.</summary>
    public int ConcurrentDeliveryLimit { get; set; }

    int ReceiveSettings.ConcurrentMessageLimit => Transport.GetConcurrentMessageLimit();

    /// <summary>Gets the consumer group used to coordinate partition ownership.</summary>
    public string ConsumerGroup { get; }
    /// <summary>Gets the Event Hub entity name.</summary>
    public string EventHubName { get; }

    /// <summary>Creates the receive-endpoint context after applying endpoint specifications.</summary>
    /// <returns>The Event Hubs receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateEventHubReceiveContext();
    }

    /// <summary>Builds the processor middleware, receive transport, and receive endpoint.</summary>
    /// <returns>The configured Event Hubs receive endpoint.</returns>
    public ReceiveEndpoint Build()
    {
        var context = CreateEventHubReceiveContext();

        _processorConfigurator.UseFilter(new EventHubBlobContainerFactoryFilter(_blobClient.Value));
        _processorConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<ProcessorContext>(context));
        _processorConfigurator.UseFilter(new EventHubConsumerFilter(context));

        IPipe<ProcessorContext> processorPipe = _processorConfigurator.Build();

        var transport = new ReceiveTransport<ProcessorContext>(_busInstance.HostConfiguration, context, () => context.ContextSupervisor,
            processorPipe);

        return new ReceiveEndpoint(transport, context);
    }

    IEventHubReceiveEndpointContext CreateEventHubReceiveContext()
    {
        var configureOptions = _configureOptions;
        var builder = new EventHubReceiveEndpointBuilder(_hostConfiguration, _busInstance, this, this,
            () => CreateEventProcessorClient(configureOptions), _partitionClosingHandler, _partitionInitializingHandler);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    BlobContainerClient CreateBlobClient()
    {
        return EventHubCheckpointContainerClientFactory.Create(_storageSettings, ContainerName);
    }

    EventProcessorClient CreateEventProcessorClient(Action<EventProcessorClientOptions>? configureOptions)
    {
        var options = new EventProcessorClientOptions();
        configureOptions?.Invoke(options);

        EventProcessorClient client;
        if (!string.IsNullOrWhiteSpace(_hostSettings.ConnectionString))
            client = new EventProcessorClient(_blobClient.Value, ConsumerGroup, _hostSettings.ConnectionString, EventHubName, options);
        else
        {
            string fullyQualifiedNamespace = _hostSettings.FullyQualifiedNamespace
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The Event Hubs namespace is not configured.", "Correct the named configuration before starting the host"));
            Azure.Core.TokenCredential credential = _hostSettings.TokenCredential
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The Event Hubs token credential is not configured.", "Correct the named configuration before starting the host"));
            client = new EventProcessorClient(_blobClient.Value, ConsumerGroup, fullyQualifiedNamespace, EventHubName, credential, options);
        }

        return client;
    }

    /// <summary>Determines whether endpoint construction has begun or the base configuration is already locked.</summary>
    /// <returns><see langword="true" /> once the Blob client has been created or base configuration is locked; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _blobClient.IsValueCreated || base.IsAlreadyConfigured();
    }
}
