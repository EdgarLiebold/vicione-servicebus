using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Creates an Event Hubs receive-endpoint context and attaches its transport settings as payloads.</summary>
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

    /// <summary>Creates a builder from the completed endpoint and processor configuration.</summary>
    /// <param name="hostConfiguration">The Event Hubs rider host configuration.</param>
    /// <param name="busInstance">The bus instance that will own the endpoint.</param>
    /// <param name="configuration">The receive endpoint's pipe configuration.</param>
    /// <param name="receiveSettings">The Event Hubs concurrency and checkpoint settings.</param>
    /// <param name="clientFactory">Creates the Azure SDK event processor client.</param>
    /// <param name="partitionClosingHandler">The optional application partition-closing handler.</param>
    /// <param name="partitionInitializingHandler">The optional application partition-initializing handler.</param>
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

    /// <summary>Creates the Event Hubs receive-endpoint context and registers topology and receive settings as payloads.</summary>
    /// <returns>The initialized receive-endpoint context.</returns>
    public IEventHubReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var context = new EventHubReceiveEndpointContext(_hostConfiguration, _busInstance, _configuration,
            _clientFactory, _partitionClosingHandler, _partitionInitializingHandler);

        context.GetOrAddPayload(() => _busInstance.HostConfiguration.Topology);
        context.AddOrUpdatePayload(() => _receiveSettings, _ => _receiveSettings);

        return context;
    }
}
