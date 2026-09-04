using System;
using System.Collections.Concurrent;
using System.Threading;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>
/// Provides a pending confirmation collection implementation.
/// </summary>
public class PendingConfirmationCollection :
    IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly ConcurrentDictionary<PartitionOffset, IPendingConfirmation> _confirmations;
    readonly CancellationTokenRegistration? _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public PendingConfirmationCollection(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _confirmations = new ConcurrentDictionary<PartitionOffset, IPendingConfirmation>();

        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(Cancel);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _registration?.Dispose();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <returns>The result of the operation.</returns>
    public IPendingConfirmation Add(ProcessEventArgs eventArgs)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var pendingConfirmation = new PendingConfirmation(eventArgs);
        return _confirmations.AddOrUpdate(eventArgs, key => pendingConfirmation, (key, existing) =>
        {
            existing.Faulted($"Duplicate key: {key} on EventHub: {eventArgs.Partition.EventHubName}");

            return pendingConfirmation;
        });
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="partitionOffset">The partition offset value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public void Faulted(PartitionOffset partitionOffset, Exception exception)
    {
        if (_confirmations.TryRemove(partitionOffset, out var confirmation))
            confirmation.Faulted(exception);
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="partitionOffset">The partition offset value.</param>
    public void Complete(PartitionOffset partitionOffset)
    {
        if (_confirmations.TryRemove(partitionOffset, out var confirmation))
            confirmation.Complete();
    }

    /// <summary>
    /// Determines whether the current value can celed.
    /// </summary>
    /// <param name="partitionOffset">The partition offset value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void Canceled(PartitionOffset partitionOffset, CancellationToken cancellationToken)
    {
        if (_confirmations.TryRemove(partitionOffset, out var confirmation))
            confirmation.Canceled(cancellationToken);
    }

    void Cancel()
    {
        foreach (var partitionOffset in _confirmations.Keys)
            Canceled(partitionOffset, _cancellationToken);
    }
}
