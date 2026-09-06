using System;
using System.Collections.Concurrent;
using System.Threading;
using Azure.Messaging.EventHubs.Processor;

namespace ViciOne.ServiceBus.EventHubs.Checkpoints;

/// <summary>Tracks unconfirmed Event Hubs events by partition and offset.</summary>
public class PendingConfirmationCollection :
    IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly ConcurrentDictionary<PartitionOffset, IPendingConfirmation> _confirmations;
    readonly CancellationTokenRegistration? _registration;

    /// <summary>Creates a collection that cancels all pending confirmations when the supplied token is canceled.</summary>
    /// <param name="cancellationToken">The token governing pending confirmation lifetime.</param>
    public PendingConfirmationCollection(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _confirmations = new ConcurrentDictionary<PartitionOffset, IPendingConfirmation>();

        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(Cancel);
    }

    /// <summary>Removes the cancellation registration owned by the collection.</summary>
    public void Dispose()
    {
        _registration?.Dispose();
    }

    /// <summary>Adds a pending confirmation for an event, faulting any duplicate entry it replaces.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <returns>The newly registered pending confirmation.</returns>
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

    /// <summary>Removes and faults the matching pending confirmation.</summary>
    /// <param name="partitionOffset">The partition offset.</param>
    /// <param name="exception">The receive-pipeline failure.</param>
    public void Faulted(PartitionOffset partitionOffset, Exception exception)
    {
        if (_confirmations.TryRemove(partitionOffset, out var confirmation))
            confirmation.Faulted(exception);
    }

    /// <summary>Removes and completes the matching pending confirmation.</summary>
    /// <param name="partitionOffset">The partition offset.</param>
    public void Complete(PartitionOffset partitionOffset)
    {
        if (_confirmations.TryRemove(partitionOffset, out var confirmation))
            confirmation.Complete();
    }

    /// <summary>Removes and cancels the matching pending confirmation.</summary>
    /// <param name="partitionOffset">The partition offset.</param>
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
