using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Provides readiness, completion, and coordinated stop signals for a supervised component.</summary>
public class Agent :
    IAgent
{
    readonly TaskCompletionSource<bool> _completed;
    readonly object _lifecycleLock;
    readonly TaskCompletionSource<bool> _ready;
    readonly CancellationTokenSource _stopped;
    readonly CancellationTokenSource _stopping;

    Task? _setCompleted;
    long _setCompletedVersion;
    Task? _setReady;
    long _setReadyVersion;
    Task? _stopTask;
    int _stopState;

    /// <summary>Initializes an active agent whose readiness and completion are not yet signaled.</summary>
    public Agent()
    {
        _lifecycleLock = new object();
        _ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _stopped = new CancellationTokenSource();
        _stopping = new CancellationTokenSource();
    }

    /// <summary>Gets whether the agent has begun stopping.</summary>
    protected bool IsStopping => Volatile.Read(ref _stopState) != 0;

    /// <summary>Gets whether a stop attempt completed successfully.</summary>
    protected bool IsStopped => Volatile.Read(ref _stopState) == 2;

    /// <summary>Gets whether the readiness signal has reached a terminal state.</summary>
    protected bool IsAlreadyReady => _ready.Task.IsCompleted;

    /// <summary>Gets whether the completion signal has reached a terminal state.</summary>
    protected bool IsAlreadyCompleted => _completed.Task.IsCompleted;

    /// <inheritdoc />
    public Task Ready => _ready.Task;

    /// <inheritdoc />
    public Task Completed => _completed.Task;

    /// <inheritdoc />
    public CancellationToken Stopping => _stopping.Token;

    /// <inheritdoc />
    public CancellationToken Stopped => _stopped.Token;

    /// <inheritdoc />
    /// <param name="context">The reason and cancellation budget for the stop attempt.</param>
    /// <param name="cancellationToken">The token that cancels this caller's wait.</param>
    public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        Task stopTask;
        TaskCompletionSource<bool>? stopCompletion = null;
        bool signalStopping = false;

        lock (_lifecycleLock)
        {
            if (_stopTask != null)
                stopTask = _stopTask;
            else if (_stopState == 2)
                return Task.CompletedTask;
            else
            {
                cancellationToken.ThrowIfCancellationRequested();

                stopCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                stopTask = _stopTask = stopCompletion.Task;

                if (_stopState == 0)
                {
                    Volatile.Write(ref _stopState, 1);
                    signalStopping = true;
                }
            }
        }

        if (stopCompletion != null)
            _ = ExecuteStopAttemptAsync(context, stopCompletion, signalStopping);

        return cancellationToken.CanBeCanceled && !stopTask.IsCompleted
            ? stopTask.WaitAsync(cancellationToken)
            : stopTask;
    }

    async Task ExecuteStopAttemptAsync(StopContext context, TaskCompletionSource<bool> stopCompletion, bool signalStopping)
    {
        try
        {
            if (signalStopping)
                SignalLifecycleToken(_stopping);

            await StopAgentAsync(context).ConfigureAwait(false);

            Volatile.Write(ref _stopState, 2);
            SignalLifecycleToken(_stopped);
            stopCompletion.TrySetResult(true);
        }
        catch (OperationCanceledException exception)
        {
            ClearFailedStopAttempt(stopCompletion.Task);
            stopCompletion.TrySetCanceled(exception.CancellationToken);
        }
        catch (Exception exception)
        {
            ClearFailedStopAttempt(stopCompletion.Task);
            stopCompletion.TrySetException(exception);
        }
    }

    void ClearFailedStopAttempt(Task stopTask)
    {
        lock (_lifecycleLock)
        {
            if (ReferenceEquals(_stopTask, stopTask) && _stopState != 2)
                _stopTask = null;
        }
    }

    static void SignalLifecycleToken(CancellationTokenSource source)
    {
        try
        {
            source.Cancel(throwOnFirstException: false);
        }
        catch (AggregateException)
        {
            // Lifecycle-token callbacks observe state changes but do not control the owned stop operation.
        }
    }

    /// <summary>Stops resources owned by the agent.</summary>
    /// <param name="context">The reason and cancellation budget for the stop attempt.</param>
    /// <returns>The resource-release operation for this agent.</returns>
    protected virtual Task StopAgentAsync(StopContext context)
    {
        _completed.TrySetResult(true);

        return Task.CompletedTask;
    }

    /// <summary>Signals that the agent is ready.</summary>
    public virtual void SetReady()
    {
        _ready.TrySetResult(true);
    }

    /// <summary>Faults the readiness signal because the agent cannot become ready.</summary>
    /// <param name="exception">The failure that prevented readiness.</param>
    public virtual void SetNotReady(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _ready.TrySetException(exception);
    }

    /// <summary>Transfers a task's terminal state to the readiness signal.</summary>
    /// <param name="readyTask">The task that determines readiness.</param>
    protected void SetReady(Task readyTask)
    {
        ArgumentNullException.ThrowIfNull(readyTask);

        lock (_ready)
        {
            if (_setReady != null)
            {
                // A completed readiness signal is already authoritative.
                if (_setReady.IsCompleted)
                {
                    readyTask.IgnoreUnobservedExceptions();

                    return;
                }
            }

            if (_ready.Task.IsCompleted)
            {
                readyTask.IgnoreUnobservedExceptions();

                return;
            }

            _setReady = readyTask;
            long version = ++_setReadyVersion;

            void OnCompleted(Task task)
            {
                lock (_ready)
                {
                    if (version != _setReadyVersion || _ready.Task.IsCompleted)
                    {
                        if (task.IsFaulted)
                            _ = task.Exception;

                        return;
                    }

                    _ready.TrySetFromTask(task, true);
                }
            }

            readyTask.ContinueWith(OnCompleted, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    /// <summary>Transfers a task's terminal state to the completion signal.</summary>
    /// <param name="completedTask">The task that determines completion.</param>
    protected void SetCompleted(Task completedTask)
    {
        ArgumentNullException.ThrowIfNull(completedTask);

        lock (_completed)
        {
            if (_setCompleted != null)
            {
                // A completed terminal signal is already authoritative.
                if (_setCompleted.IsCompleted)
                {
                    completedTask.IgnoreUnobservedExceptions();

                    return;
                }
            }

            if (_completed.Task.IsCompleted)
            {
                completedTask.IgnoreUnobservedExceptions();

                return;
            }

            _setCompleted = completedTask;
            long version = ++_setCompletedVersion;

            void OnCompleted(Task task)
            {
                lock (_completed)
                {
                    if (version != _setCompletedVersion || _completed.Task.IsCompleted)
                    {
                        if (task.IsFaulted)
                            _ = task.Exception;

                        return;
                    }

                    _completed.TrySetFromTask(task, true);
                }
            }

            completedTask.ContinueWith(OnCompleted, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    /// <summary>Transfers a failed or canceled task to readiness and completes the lifecycle.</summary>
    /// <param name="task">The task that carries the terminal failure or cancellation.</param>
    protected void SetFaulted(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsCanceled || task.IsFaulted)
            _ready.TrySetFromTask(task, true);
        else
            _ready.TrySetException(new InvalidOperationException("The context faulted but no exception was present."));

        _completed.TrySetResult(true);
    }
}
