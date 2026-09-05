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

/// <summary>
/// Provides an event hub receive endpoint configurator implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="hostSettings">The host settings value.</param>
    /// <param name="storageSettings">The storage settings value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
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

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _endpointConfiguration.HostAddress;

    /// <summary>
    /// Gets or sets the container name value.
    /// </summary>
    public string ContainerName
    {
        get => _containerName ?? EventHubName;
        set
        {
            _containerName = value ?? throw new ArgumentNullException(nameof(value));

            Changed(nameof(ContainerName));
        }
    }

    /// <summary>
    /// Gets or sets the checkpoint interval value.
    /// </summary>
    public TimeSpan CheckpointInterval { get; set; }
    /// <summary>
    /// Gets or sets the checkpoint message limit value.
    /// </summary>
    public ushort CheckpointMessageLimit { get; set; }
    /// <summary>
    /// Gets or sets the checkpoint message count value.
    /// </summary>
    public ushort CheckpointMessageCount { get; set; }

    /// <summary>
    /// Gets or sets the configure options value.
    /// </summary>
    public Action<EventProcessorClientOptions> ConfigureOptions
    {
        set => _configureOptions = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Performs the on partition closing operation.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public void OnPartitionClosing(Func<PartitionClosingEventArgs, Task> handler)
    {
        if (_partitionClosingHandler != null)
            throw new InvalidOperationException("Partition closing event handler may not be specified more than once.");
        _partitionClosingHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Performs the on partition initializing operation.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    public void OnPartitionInitializing(Func<PartitionInitializingEventArgs, Task> handler)
    {
        if (_partitionInitializingHandler != null)
            throw new InvalidOperationException("Partition initializing event handler may not be specified more than once.");
        _partitionInitializingHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public override Uri InputAddress => _endpointConfiguration.InputAddress;
    /// <summary>
    /// Gets or sets the concurrent delivery limit value.
    /// </summary>
    public int ConcurrentDeliveryLimit { get; set; }

    int ReceiveSettings.ConcurrentMessageLimit => Transport.GetConcurrentMessageLimit();

    /// <summary>
    /// Gets the consumer group value.
    /// </summary>
    public string ConsumerGroup { get; }
    /// <summary>
    /// Gets the event hub name value.
    /// </summary>
    public string EventHubName { get; }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateEventHubReceiveContext();
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
        var builder = new EventHubReceiveEndpointBuilder(_hostConfiguration, _busInstance, this, this,
            CreateEventProcessorClient, _partitionClosingHandler, _partitionInitializingHandler);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    BlobContainerClient CreateBlobClient()
    {
        var blobClientOptions = new BlobClientOptions();
        _storageSettings.Configure?.Invoke(blobClientOptions);

        var containerName = ContainerName;
        if (!string.IsNullOrWhiteSpace(_storageSettings.ConnectionString))
            return new BlobContainerClient(_storageSettings.ConnectionString, containerName, blobClientOptions);

        Uri containerUri = _storageSettings.ContainerUri
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The Event Hub checkpoint storage container URI is not configured.", "Correct the named configuration before starting the host"));
        var uri = new Uri(containerUri, containerName);
        if (_storageSettings.TokenCredential != null)
            return new BlobContainerClient(uri, _storageSettings.TokenCredential, blobClientOptions);

        return _storageSettings.SharedKeyCredential != null
            ? new BlobContainerClient(uri, _storageSettings.SharedKeyCredential, blobClientOptions)
            : new BlobContainerClient(containerUri, blobClientOptions);
    }

    EventProcessorClient CreateEventProcessorClient()
    {
        var options = new EventProcessorClientOptions();
        _configureOptions?.Invoke(options);

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

    /// <summary>
    /// Determines whether already configured.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _blobClient.IsValueCreated || base.IsAlreadyConfigured();
    }
}
