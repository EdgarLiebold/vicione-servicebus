using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for processor context.
/// </summary>
public interface ProcessorContext :
    PipeContext
{
    /// <summary>
    /// Gets the log context value.
    /// </summary>
    ILogContext LogContext { get; }
    /// <summary>
    /// Gets client.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    EventProcessorClient GetClient(ProcessorClientBuilderContext context);
    /// <summary>
    /// Performs the release client operation.
    /// </summary>
    /// <param name="processorLockContext">The processor lock context value.</param>
    void ReleaseClient(ProcessorClientBuilderContext processorLockContext);
}
