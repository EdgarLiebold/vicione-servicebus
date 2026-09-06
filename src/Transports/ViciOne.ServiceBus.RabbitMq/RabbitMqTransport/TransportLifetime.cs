using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Coordinates the lifetime of a RabbitMQ channel or connection with its active operations.
/// Invalidation rejects new leases immediately, retains the broker close reason, and schedules
/// disposal only after the final active lease has been released.
/// </summary>
internal sealed class TransportLifetime :
    IAsyncDisposable
{
    readonly Func<Task> _disposeSubject;
    readonly object _lock = new object();
    readonly Action<Func<Task>> _scheduleSubjectDisposal;
    readonly string _subject;

    /// <summary>
    /// Completes once subject disposal finishes. The result carries any disposal exception so the
    /// scheduled disposal task itself cannot become faulted and an awaiting owner can rethrow it.
    /// </summary>
    readonly TaskCompletionSource<Exception?> _disposed =
        new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);

    ShutdownEventArgs? _closeReason;
    int _active;
    int _disposeStarted;
    bool _invalidated;

    /// <summary>Creates lifetime coordination for an owned channel or connection.</summary>
    /// <param name="subject">The subject description used when no broker close reason is available.</param>
    /// <param name="disposeSubject">The asynchronous operation that disposes the subject.</param>
    public TransportLifetime(string subject, Func<Task> disposeSubject)
        : this(subject, disposeSubject, QueueSubjectDisposal)
    {
    }

    internal TransportLifetime(string subject, Func<Task> disposeSubject, Action<Func<Task>> scheduleSubjectDisposal)
    {
        _subject = subject;
        _disposeSubject = disposeSubject ?? throw new ArgumentNullException(nameof(disposeSubject));
        _scheduleSubjectDisposal = scheduleSubjectDisposal ?? throw new ArgumentNullException(nameof(scheduleSubjectDisposal));
    }

    /// <summary>Gets the first broker close reason supplied during invalidation, if any.</summary>
    public ShutdownEventArgs? CloseReason
    {
        get
        {
            lock (_lock)
                return _closeReason;
        }
    }

    /// <summary>Attempts to acquire an operation lease before invalidation or disposal begins.</summary>
    /// <param name="lease">Receives a lease that must be disposed after the operation finishes.</param>
    /// <returns><see langword="true"/> when a lease was acquired; otherwise, <see langword="false"/>.</returns>
    public bool TryLease([NotNullWhen(true)] out Lease? lease)
    {
        lock (_lock)
        {
            if (_invalidated || Volatile.Read(ref _disposeStarted) != 0)
            {
                lease = null;
                return false;
            }

            _active++;
        }

        lease = new Lease(this);
        return true;
    }

    /// <summary>Creates an interruption exception from the retained broker close reason or a transport-generated fallback.</summary>
    /// <returns>An exception describing why the channel or connection is unavailable.</returns>
    public OperationInterruptedException NotAvailable()
    {
        var reason = CloseReason;

        return reason != null
            ? new OperationInterruptedException(reason)
            : new OperationInterruptedException(
                new ShutdownEventArgs(ShutdownInitiator.Library, 491, $"The {_subject} is no longer available"));
    }

    /// <summary>Rejects new leases, retains the first close reason, and schedules disposal when no leases remain.</summary>
    /// <param name="reason">The broker close reason, or <see langword="null"/> for owner-initiated disposal.</param>
    public void Invalidate(ShutdownEventArgs? reason)
    {
        bool idle;

        lock (_lock)
        {
            if (_closeReason == null && reason != null)
                _closeReason = reason;

            _invalidated = true;
            idle = _active == 0;
        }

        if (idle)
            ScheduleDispose();
    }

    /// <summary>Invalidates the subject, waits for its scheduled disposal, and rethrows any disposal failure.</summary>
    /// <returns>A value task that completes when the subject has been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        Invalidate(null);

        var failure = await _disposed.Task.ConfigureAwait(false);

        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    void Release()
    {
        bool idle;

        lock (_lock)
        {
            _active--;
            idle = _invalidated && _active == 0;
        }

        if (idle)
            ScheduleDispose();
    }

    void ScheduleDispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        // Schedule disposal outside the shutdown callback; _disposed remains the single completion contract.
        _scheduleSubjectDisposal(DisposeSubjectAsync);
    }

    static void QueueSubjectDisposal(Func<Task> disposeSubject) =>
        ThreadPool.QueueUserWorkItem(state => { _ = disposeSubject(); });

    /// <summary>Disposes the subject once and records success or failure in <see cref="_disposed"/>.</summary>
    /// <returns>A task that completes after the disposal outcome has been recorded.</returns>
    async Task DisposeSubjectAsync()
    {
        Exception? failure = null;

        try
        {
            await _disposeSubject().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;

            LogContext.Error?.Log(exception, "Disposing the {Subject} faulted after its last operation finished", _subject);
        }

        _disposed.TrySetResult(failure);
    }


    /// <summary>Represents one active operation and releases its lifetime claim at most once.</summary>
    public sealed class Lease :
        IDisposable
    {
        readonly TransportLifetime _lifetime;
        int _released;

        internal Lease(TransportLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                _lifetime.Release();
        }
    }
}
