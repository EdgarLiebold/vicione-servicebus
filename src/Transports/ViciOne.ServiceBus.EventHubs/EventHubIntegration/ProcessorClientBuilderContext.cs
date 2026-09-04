using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for processor client builder context.
/// </summary>
public interface ProcessorClientBuilderContext
{
    /// <summary>
    /// Performs the on partition initializing operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the on partition closing operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default);
}
