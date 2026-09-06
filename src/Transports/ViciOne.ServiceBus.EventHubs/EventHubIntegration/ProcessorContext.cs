using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns an Event Hubs processor client and its partition lifecycle subscriptions.</summary>
public interface ProcessorContext :
    PipeContext
{
    /// <summary>Gets the receive endpoint's logging context.</summary>
    ILogContext LogContext { get; }
    /// <summary>Registers partition callbacks and leases the processor client to a lock context.</summary>
    /// <param name="context">The callback target for partition initialization and closure.</param>
    /// <returns>The owned processor client.</returns>
    EventProcessorClient GetClient(ProcessorClientBuilderContext context);
    /// <summary>Unsubscribes the partition callbacks associated with the current client lease.</summary>
    /// <param name="processorLockContext">The callback target whose lease is being released.</param>
    void ReleaseClient(ProcessorClientBuilderContext processorLockContext);
}
