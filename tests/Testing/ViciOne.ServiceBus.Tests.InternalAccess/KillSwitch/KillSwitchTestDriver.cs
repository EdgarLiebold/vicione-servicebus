using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports.Components;

namespace ViciOne.ServiceBus.Tests.InternalAccess.KillSwitch;

/// <summary>
/// Test-only driver for the internal kill-switch state machine. It exposes observations and endpoint
/// effects without reproducing any decision rule from the product.
/// </summary>
public sealed class KillSwitchTestDriver
{
    private readonly FakeEndpoint _endpoint;
    private readonly ViciOne.ServiceBus.Transports.Components.KillSwitch _killSwitch;

    public KillSwitchTestDriver(KillSwitchOptions options, ILogContext logContext)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logContext);

        KillSwitchSettings settings = options.CreateSettings();
        Settings = new KillSwitchSettingsSnapshot(
            settings.ActivationThreshold,
            settings.TripThresholdRatio,
            settings.TrackingPeriod,
            settings.RestartDelay,
            settings.TimeProvider);
        _endpoint = new FakeEndpoint(logContext);
        _killSwitch = new ViciOne.ServiceBus.Transports.Components.KillSwitch(settings);
        _killSwitch.Attach(_endpoint);
    }

    public KillSwitchSettingsSnapshot Settings { get; }
    public int ConnectCount => _endpoint.ConnectCount;
    public int PauseCount => _endpoint.PauseCount;
    public int StartCount => _endpoint.StartCount;
    public string[] Events => _endpoint.Events;
    public ILogContext? LogContextSeenByLastPause => _endpoint.LogContextSeenByLastPause;
    public ILogContext? LogContextSeenByLastRestart => _endpoint.LogContextSeenByLastRestart;
    public Task RecoveryTask => _killSwitch.RecoveryTask;

    public KillSwitchTestSnapshot Snapshot
    {
        get
        {
            KillSwitchSnapshot snapshot = _killSwitch.Snapshot;
            return new KillSwitchTestSnapshot(
                MapState(snapshot.State),
                snapshot.AttemptCount,
                snapshot.SuccessCount,
                snapshot.FailureCount,
                snapshot.RecoveryActive);
        }
    }

    public async Task ObserveSuccessAsync()
    {
        await _killSwitch.PreConsumeAsync<object>(null!).ConfigureAwait(false);
        await _killSwitch.PostConsumeAsync<object>(null!).ConfigureAwait(false);
    }

    public async Task ObserveFailureAsync(Exception exception)
    {
        await _killSwitch.PreConsumeAsync<object>(null!).ConfigureAwait(false);
        await _killSwitch.ConsumeFaultAsync<object>(null!, exception).ConfigureAwait(false);
    }

    public Task ObserveAttemptAsync() => _killSwitch.PreConsumeAsync<object>(null!);

    public Task ObserveMatchingFailureAsync(Exception exception) => _killSwitch.ConsumeFaultAsync<object>(null!, exception);

    public async Task ObserveConsumerAndRoutingSlipCallbacksAsync(Exception executeException)
    {
        await _killSwitch.PreConsumeAsync<object>(null!).ConfigureAwait(false);
        await _killSwitch.PostConsumeAsync<object>(null!).ConfigureAwait(false);
        await _killSwitch.PreExecuteAsync<TestExecuteActivity, ActivityArguments>(null!).ConfigureAwait(false);
        await _killSwitch.ExecuteFaultAsync<TestExecuteActivity, ActivityArguments>(null!, executeException).ConfigureAwait(false);
        await _killSwitch.PreCompensateAsync<TestCompensateActivity, ActivityLog>(null!).ConfigureAwait(false);
        await _killSwitch.PostCompensateAsync<TestCompensateActivity, ActivityLog>(null!).ConfigureAwait(false);
    }

    public Task StopEndpointAsync() => _killSwitch.StoppingAsync(null!);

    public void Rearm()
    {
        _killSwitch.Attach(_endpoint);
    }

    public void AttachDifferentEndpoint()
    {
        _killSwitch.Attach(new FakeEndpoint(_endpoint.LogContext));
    }

    public void EnqueuePauseFailure(Exception exception) => _endpoint.EnqueuePauseFailure(exception);

    public void EnqueueStartFailure(Exception exception) => _endpoint.EnqueueStartFailure(exception);

    public Task WaitForPauseCountAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
        _endpoint.WaitForPauseCountAsync(count, timeout, cancellationToken);

    public Task WaitForStartCountAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
        _endpoint.WaitForStartCountAsync(count, timeout, cancellationToken);

    private sealed class FakeEndpoint(ILogContext logContext) : IKillSwitchEndpoint
    {
        private readonly List<string> _events = [];
        private readonly object _lock = new();
        private readonly Queue<Exception> _pauseFailures = [];
        private readonly Queue<Exception> _startFailures = [];
        private TaskCompletionSource<bool> _pauseChanged = NewSignal();
        private TaskCompletionSource<bool> _startChanged = NewSignal();
        private int _connectCount;
        private int _pauseCount;
        private int _startCount;

        public object Identity => this;
        public Uri InputAddress { get; } = new("loopback://localhost/kill-switch");
        public ILogContext LogContext { get; } = logContext;
        public ILogContext? LogContextSeenByLastPause { get; private set; }
        public ILogContext? LogContextSeenByLastRestart { get; private set; }
        public int ConnectCount => Volatile.Read(ref _connectCount);
        public int PauseCount => Volatile.Read(ref _pauseCount);
        public int StartCount => Volatile.Read(ref _startCount);

        public string[] Events
        {
            get
            {
                lock (_lock)
                    return _events.ToArray();
            }
        }

        public void ConnectConsumeObserver(IConsumeObserver observer) => Interlocked.Increment(ref _connectCount);

        public Task PauseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Exception? failure;
            TaskCompletionSource<bool> changed;
            lock (_lock)
            {
                _events.Add("pause");
                _pauseCount++;
                LogContextSeenByLastPause = ViciOne.ServiceBus.Advanced.LogContext.Current;
                failure = _pauseFailures.TryDequeue(out Exception? queued) ? queued : null;
                changed = _pauseChanged;
                _pauseChanged = NewSignal();
            }

            changed.TrySetResult(true);
            return failure is null ? Task.CompletedTask : Task.FromException(failure);
        }

        public Task<ReceiveEndpointHandle> RestartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Exception? failure;
            TaskCompletionSource<bool> changed;
            lock (_lock)
            {
                _events.Add("start");
                _startCount++;
                LogContextSeenByLastRestart = ViciOne.ServiceBus.Advanced.LogContext.Current;
                failure = _startFailures.TryDequeue(out Exception? queued) ? queued : null;
                changed = _startChanged;
                _startChanged = NewSignal();
            }

            changed.TrySetResult(true);
            if (failure is not null)
                return Task.FromException<ReceiveEndpointHandle>(failure);

            return Task.FromResult<ReceiveEndpointHandle>(new ReadyHandle());
        }

        public void EnqueuePauseFailure(Exception exception)
        {
            lock (_lock)
                _pauseFailures.Enqueue(exception);
        }

        public void EnqueueStartFailure(Exception exception)
        {
            lock (_lock)
                _startFailures.Enqueue(exception);
        }

        public Task WaitForPauseCountAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            WaitForCountAsync(PauseState, count, timeout, cancellationToken);

        public Task WaitForStartCountAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            WaitForCountAsync(StartState, count, timeout, cancellationToken);

        private (int Count, Task Changed) PauseState()
        {
            lock (_lock)
                return (_pauseCount, _pauseChanged.Task);
        }

        private (int Count, Task Changed) StartState()
        {
            lock (_lock)
                return (_startCount, _startChanged.Task);
        }

        private static async Task WaitForCountAsync(
            Func<(int Count, Task Changed)> snapshot,
            int expected,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expected);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(timeout);

            while (true)
            {
                (int count, Task changed) = snapshot();
                if (count >= expected)
                    return;

                await changed.WaitAsync(timeoutCancellation.Token).ConfigureAwait(false);
            }
        }
    }

    private sealed class ReadyHandle : ReceiveEndpointHandle
    {
        public Task<ReceiveEndpointReady> Ready { get; } = Task.FromResult<ReceiveEndpointReady>(new ReadyEvent());

        public Task StopAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
    }

    private sealed class ReadyEvent : ReceiveEndpointReady
    {
        public bool IsStarted => true;
        public Uri InputAddress { get; } = new("loopback://localhost/kill-switch");
        public IReceiveEndpoint ReceiveEndpoint => null!;
    }

    private sealed record ActivityArguments;
    private sealed record ActivityLog;

    private sealed class TestExecuteActivity : IExecuteActivity<ActivityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) => throw new NotSupportedException();
    }

    private sealed class TestCompensateActivity : ICompensateActivity<ActivityLog>
    {
        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) => throw new NotSupportedException();
    }

    private static TaskCompletionSource<bool> NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static KillSwitchTestState MapState(KillSwitchState state) => state switch
    {
        KillSwitchState.Initial => KillSwitchTestState.Initial,
        KillSwitchState.Running => KillSwitchTestState.Running,
        KillSwitchState.Stopping => KillSwitchTestState.Stopping,
        KillSwitchState.Paused => KillSwitchTestState.Paused,
        KillSwitchState.Starting => KillSwitchTestState.Starting,
        KillSwitchState.VerifyingRecovery => KillSwitchTestState.VerifyingRecovery,
        KillSwitchState.Terminated => KillSwitchTestState.Terminated,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown product kill-switch state."),
    };
}

public sealed record KillSwitchSettingsSnapshot(
    int ActivationThreshold,
    double TripThresholdRatio,
    TimeSpan TrackingPeriod,
    TimeSpan RestartDelay,
    TimeProvider TimeProvider);

public readonly record struct KillSwitchTestSnapshot(
    KillSwitchTestState State,
    int AttemptCount,
    int SuccessCount,
    int FailureCount,
    bool RecoveryActive);

public enum KillSwitchTestState
{
    Initial,
    Running,
    Stopping,
    Paused,
    Starting,
    VerifyingRecovery,
    Terminated
}
