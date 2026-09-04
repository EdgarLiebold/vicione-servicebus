using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides an async inactivity observer implementation.
/// </summary>
public class AsyncInactivityObserver :
    IDisposable,
    IInactivityObserver
{
    readonly Lazy<Task> _inactivityTask;
    readonly TaskCompletionSource<bool> _inactivityTaskSource;
    readonly CancellationTokenSource _inactivityTokenSource;
    readonly HashSet<IInactivityObservationSource> _sources;
    readonly TimeProvider _timeProvider;
    int _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken)
        : this(timeout, cancellationToken, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _inactivityTaskSource = TaskCompletionSources.Create<bool>();
        _inactivityTask = new Lazy<Task>(() => TimeoutTaskAsync(timeout, cancellationToken));

        _sources = new HashSet<IInactivityObservationSource>();
        _inactivityTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets the inactivity task value.
    /// </summary>
    public Task InactivityTask => _inactivityTask.Value;

    /// <summary>
    /// Gets the inactivity token value.
    /// </summary>
    public CancellationToken InactivityToken => _inactivityTokenSource.Token;

    /// <summary>
    /// Connects ed.
    /// </summary>
    /// <param name="source">The source value.</param>
    public void Connected(IInactivityObservationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_sources)
            _sources.Add(source);
    }

    /// <summary>
    /// Performs the no activity operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NoActivityAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return CheckSourceActivityAsync();
    }

    /// <summary>
    /// Performs the force inactive operation.
    /// </summary>
    public void ForceInactive()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        _inactivityTaskSource.TrySetResult(true);
        _inactivityTokenSource.Cancel();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
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
            // The completion may already have been forced before this task was materialized, because the task
            // is created lazily on the first access. Waiting an interval first would ignore that.
            var inActive = _inactivityTaskSource.Task.IsCompleted;
            while (!inActive)
            {
                using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Task delay = Task.Delay(timeout, _timeProvider, delayCancellation.Token);

                Task completed = await Task.WhenAny(delay, _inactivityTaskSource.Task).ConfigureAwait(false);
                if (completed != delay)
                {
                    // Forced while this interval was running. The interval is no longer needed, so it is
                    // cancelled instead of being left to run to its end.
                    delayCancellation.Cancel();
                    delay.IgnoreUnobservedExceptions();
                    break;
                }

                await delay.ConfigureAwait(false);

                inActive = await CheckSourceActivityAsync().ConfigureAwait(false);
            }

            await _inactivityTaskSource.Task.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
