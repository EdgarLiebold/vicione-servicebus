using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Receives partition lifecycle callbacks used to maintain checkpoint state around an Event Hubs processor client.</summary>
public interface ProcessorClientBuilderContext
{
    /// <summary>Initializes local state before the processor begins reading a partition.</summary>
    /// <param name="eventArgs">The Azure SDK partition-initializing arguments.</param>
    /// <param name="cancellationToken">Cancels partition initialization.</param>
    /// <returns>A task that completes after partition state is initialized.</returns>
    Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>Closes local checkpoint state after the processor stops reading a partition.</summary>
    /// <param name="eventArgs">The Azure SDK partition-closing arguments.</param>
    /// <param name="cancellationToken">Cancels partition-state closure.</param>
    /// <returns>A task that completes after partition state is closed.</returns>
    Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default);
}
