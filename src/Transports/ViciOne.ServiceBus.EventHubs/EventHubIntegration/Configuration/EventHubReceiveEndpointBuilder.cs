using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides an event hub receive endpoint builder implementation.
/// </summary>
public class EventHubReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IBusInstance _busInstance;
    readonly Func<EventProcessorClient> _clientFactory;
    readonly IReceiveEndpointConfiguration _configuration;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    readonly Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;
    readonly ReceiveSettings _receiveSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="receiveSettings">The receive settings value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="partitionClosingHandler">The partition closing handler value.</param>
    /// <param name="partitionInitializingHandler">The partition initializing handler value.</param>
    public EventHubReceiveEndpointBuilder(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance,
        IReceiveEndpointConfiguration configuration, ReceiveSettings receiveSettings,
        Func<EventProcessorClient> clientFactory,
        Func<PartitionClosingEventArgs, Task>? partitionClosingHandler,
        Func<PartitionInitializingEventArgs, Task>? partitionInitializingHandler)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _configuration = configuration;
        _receiveSettings = receiveSettings;
        _clientFactory = clientFactory;
        _partitionClosingHandler = partitionClosingHandler;
        _partitionInitializingHandler = partitionInitializingHandler;
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEventHubReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var context = new EventHubReceiveEndpointContext(_hostConfiguration, _busInstance, _configuration,
            _clientFactory, _partitionClosingHandler, _partitionInitializingHandler);

        context.GetOrAddPayload(() => _busInstance.HostConfiguration.Topology);
        context.AddOrUpdatePayload(() => _receiveSettings, _ => _receiveSettings);

        return context;
    }
}
