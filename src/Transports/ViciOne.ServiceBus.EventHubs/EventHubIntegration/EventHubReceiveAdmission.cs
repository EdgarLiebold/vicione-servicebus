using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Advanced.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Coordinates bounded receive admission with pending-checkpoint ownership and partitioned dispatch.</summary>
internal sealed class EventHubReceiveAdmission :
    IDisposable
{
    readonly IPartitionedTaskExecutor<ProcessEventArgs> _executor;
    readonly SemaphoreSlim _limit;
    readonly IProcessorLockContext _lockContext;

    /// <summary>Creates an admission boundary with the endpoint's prefetch capacity.</summary>
    /// <param name="capacity">The maximum number of admitted events awaiting or undergoing delivery.</param>
    /// <param name="lockContext">The checkpoint state that owns pending-event confirmations.</param>
    /// <param name="executor">The partition-aware delivery queue.</param>
    public EventHubReceiveAdmission(
        int capacity,
        IProcessorLockContext lockContext,
        IPartitionedTaskExecutor<ProcessEventArgs> executor)
    {
        ArgumentNullException.ThrowIfNull(lockContext);
        ArgumentNullException.ThrowIfNull(executor);

        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Must be greater than zero.");

        _limit = new SemaphoreSlim(capacity);
        _lockContext = lockContext;
        _executor = executor;
    }

    /// <summary>Disposes the admission semaphore after all accepted delivery work has drained.</summary>
    public void Dispose()
    {
        _limit.Dispose();
    }

    /// <summary>Registers an event as pending and transfers its capacity lease to accepted delivery work.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="handler">The delivery operation that owns the lease after queue admission.</param>
    /// <param name="cancellationToken">Cancels admission before the partitioned queue accepts the operation.</param>
    /// <returns>A task that completes when the partitioned queue accepts the delivery operation.</returns>
    public async Task EnqueueAsync(
        ProcessEventArgs eventArgs,
        Func<Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        await _limit.WaitAsync(cancellationToken).ConfigureAwait(false);

        bool accepted = false;

        try
        {
            await _lockContext.PendingAsync(eventArgs, cancellationToken).ConfigureAwait(false);
            await _executor.EnqueueAsync(
                    eventArgs,
                    async () =>
                    {
                        try
                        {
                            await handler().ConfigureAwait(false);
                        }
                        finally
                        {
                            _limit.Release();
                        }
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            accepted = true;
        }
        catch (OperationCanceledException exception)
        {
            CancellationToken canceledToken = exception.CancellationToken.IsCancellationRequested
                ? exception.CancellationToken
                : cancellationToken;
            _lockContext.Canceled(eventArgs, canceledToken);
            throw;
        }
        catch (Exception exception)
        {
            await _lockContext.FaultedAsync(eventArgs, exception, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (!accepted)
                _limit.Release();
        }
    }
}
