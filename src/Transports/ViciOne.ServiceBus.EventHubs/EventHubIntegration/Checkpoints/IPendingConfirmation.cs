using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>Tracks consumption completion for an event that is awaiting a partition checkpoint.</summary>
public interface IPendingConfirmation
{
    /// <summary>Gets the Event Hubs partition that supplied the event.</summary>
    PartitionContext Partition { get; }

    /// <summary>Gets the provider-defined offset of the event.</summary>
    string OffsetString { get; }

    /// <summary>Gets the task that completes when consumption succeeds, fails, or is canceled.</summary>
    Task Confirmed { get; }

    /// <summary>Marks message consumption as successful.</summary>
    void Complete();
    /// <summary>Marks message consumption as failed.</summary>
    /// <param name="exception">The failure reported by the receive pipeline.</param>
    void Faulted(Exception exception);
    /// <summary>Marks the confirmation as failed with an argument error.</summary>
    /// <param name="message">The error description.</param>
    void Faulted(string message);
    /// <summary>Marks message consumption as canceled.</summary>
    /// <param name="cancellationToken">The token that caused cancellation.</param>
    void Canceled(CancellationToken cancellationToken);

    /// <summary>Advances the provider checkpoint through this event.</summary>
    /// <param name="cancellationToken">Cancels the checkpoint update.</param>
    /// <returns>A task that completes when Event Hubs has accepted the checkpoint update.</returns>
    Task CheckpointAsync(CancellationToken cancellationToken);
}
