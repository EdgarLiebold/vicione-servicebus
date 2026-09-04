using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Defines the contract for pending confirmation.
/// </summary>
public interface IPendingConfirmation
{
    /// <summary>
    /// Gets the partition value.
    /// </summary>
    PartitionContext Partition { get; }

    /// <summary>
    /// Gets the offset string value.
    /// </summary>
    string OffsetString { get; }

    /// <summary>
    /// Gets the confirmed value.
    /// </summary>
    Task Confirmed { get; }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    void Complete();
    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    void Faulted(Exception exception);
    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    void Faulted(string message);
    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    void Canceled(CancellationToken cancellationToken);

    /// <summary>
    /// Performs the checkpoint operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CheckpointAsync(CancellationToken cancellationToken);
}
