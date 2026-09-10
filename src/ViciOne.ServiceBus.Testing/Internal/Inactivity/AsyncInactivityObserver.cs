using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Completes when every connected observation source remains inactive for a configured interval.</summary>
internal sealed class AsyncInactivityObserver :
    IDisposable,
    IInactivityObserver
{
    readonly Lazy<Task> _inactivityTask;
    readonly TaskCompletionSource<bool> _inactivityTaskSource;
    readonly CancellationTokenSource _inactivityTokenSource;
    readonly HashSet<IInactivityObservationSource> _sources;
    readonly TimeProvider _timeProvider;
    int _disposed;

    /// <summary>Creates an inactivity observer that uses the system clock.</summary>
    /// <param name="timeout">The interval after which connected sources are checked for inactivity.</param>
    /// <param name="cancellationToken">The token that ends inactivity observation.</param>
    public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken)
        : this(timeout, cancellationToken, TimeProvider.System)
    {
    }

    /// <summary>Creates an inactivity observer.</summary>
    /// <param name="timeout">The interval after which connected sources are checked for inactivity.</param>
    /// <param name="cancellationToken">The token that ends inactivity observation.</param>
    /// <param name="timeProvider">The clock used to measure inactivity intervals.</param>
    public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _inactivityTaskSource = TaskCompletionSources.Create<bool>();
        _inactivityTask = new Lazy<Task>(() => TimeoutTaskAsync(timeout, cancellationToken));

        _sources = new HashSet<IInactivityObservationSource>();
        _inactivityTokenSource = new CancellationTokenSource();
    }

    /// <summary>Gets the task that completes when all connected sources are inactive.</summary>
    public Task InactivityTask => _inactivityTask.Value;

    /// <summary>Gets the token canceled when inactivity is detected or forced.</summary>
    public CancellationToken InactivityToken => _inactivityTokenSource.Token;

    /// <summary>Registers a source whose activity participates in the inactivity decision.</summary>
    /// <param name="source">The activity source to monitor.</param>
    public void RegisterSource(IInactivityObservationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_sources)
            _sources.Add(source);
    }

    /// <summary>Checks whether every connected source is currently inactive.</summary>
    /// <param name="cancellationToken">The token used to cancel this check.</param>
    /// <returns>A task that completes after the sources have been checked.</returns>
    public Task EvaluateInactivityAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return CheckSourceActivityAsync();
    }

    /// <summary>Completes the observation as inactive without waiting for the configured interval.</summary>
    public void ForceInactive()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        _inactivityTaskSource.TrySetResult(true);
        _inactivityTokenSource.Cancel();
    }

    /// <summary>Cancels pending observation and releases its cancellation source.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _inactivityTaskSource.TrySetCanceled();
        _inactivityTokenSource.Cancel();
        _inactivityTokenSource.Dispose();
    }

    Task<bool> CheckSourceActivityAsync()
    {
        IInactivityObservationSource[] sources;
        lock (_sources)
            sources = _sources.ToArray();

        if (sources.All(x => x.IsInactive))
        {
            _inactivityTaskSource.TrySetResult(true);
            _inactivityTokenSource.Cancel();

            return TaskResults.True;
        }

        return TaskResults.False;
    }

    async Task TimeoutTaskAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            bool isInactive = _inactivityTaskSource.Task.IsCompleted;
            while (!isInactive)
            {
                using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Task delay = Task.Delay(timeout, _timeProvider, delayCancellation.Token);

                Task completed = await Task.WhenAny(delay, _inactivityTaskSource.Task).ConfigureAwait(false);
                if (completed != delay)
                {
                    delayCancellation.Cancel();
                    delay.IgnoreUnobservedExceptions();
                    break;
                }

                await delay.ConfigureAwait(false);

                isInactive = await CheckSourceActivityAsync().ConfigureAwait(false);
            }

            await _inactivityTaskSource.Task.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
