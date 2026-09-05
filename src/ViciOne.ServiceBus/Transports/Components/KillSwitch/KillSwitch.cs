using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Components;
/// <summary>
/// Owns the complete pause-and-restart lifecycle for one receive endpoint. Configuration is immutable,
/// observations and transitions share one synchronization boundary, and exactly one recovery operation
/// may exist at a time.
/// </summary>
internal sealed class KillSwitch :
    IReceiveEndpointObserver,
    IConsumeObserver,
    IActivityObserver
{
    private readonly object _lock = new();
    private readonly KillSwitchSettings _settings;
    private int _attemptCount;
    private CancellationTokenSource _lifetimeCancellation;
    private int _failureCount;
    private ILogContext? _logContext;
    private IKillSwitchEndpoint? _receiveEndpoint;
    private Task _recoveryTask;
    private bool _selfStopping;
    private KillSwitchState _state;
    private int _successCount;
    private long _windowStartedAt;

    public KillSwitch(KillSwitchSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _lifetimeCancellation = new CancellationTokenSource();
        _recoveryTask = Task.CompletedTask;
        _state = KillSwitchState.Initial;
    }

    internal KillSwitchSnapshot Snapshot
    {
        get
        {
            lock (_lock)
            {
                return new KillSwitchSnapshot(
                    _state,
                    _attemptCount,
                    _successCount,
                    _failureCount,
                    !_recoveryTask.IsCompleted);
            }
        }
    }

    internal Task RecoveryTask
    {
        get
        {
            lock (_lock)
                return _recoveryTask;
        }
    }

    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        RecordAttempt();
        return Task.CompletedTask;
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        RecordSuccess();
        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        RecordFailure(exception);
        return Task.CompletedTask;
    }

    public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class
    {
        RecordAttempt();
        return Task.CompletedTask;
    }

    public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class
    {
        RecordSuccess();
        return Task.CompletedTask;
    }

    public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class
        where TArguments : class
    {
        RecordFailure(exception);
        return Task.CompletedTask;
    }

    public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class
    {
        RecordAttempt();
        return Task.CompletedTask;
    }

    public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class
    {
        RecordSuccess();
        return Task.CompletedTask;
    }

    public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class
        where TLog : class
    {
        RecordFailure(exception);
        return Task.CompletedTask;
    }

    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        ArgumentNullException.ThrowIfNull(ready);

        if (ready.ReceiveEndpoint is not IRestartableReceiveEndpoint endpoint)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Kill Switch", "unknown", $"The kill switch requires a restartable receive endpoint, but {ready.ReceiveEndpoint.GetType().Name} does not support policy pauses.", "Correct the named configuration before starting the host"));
        }

        Attach(new RestartableReceiveEndpointKillSwitchEndpoint(endpoint));
        return Task.CompletedTask;
    }

    internal void Attach(IKillSwitchEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        CancellationTokenSource? previousLifetime = null;
        var connectObserver = false;

        lock (_lock)
        {
            if (_receiveEndpoint is not null && !ReferenceEquals(_receiveEndpoint.Identity, endpoint.Identity))
                throw new InvalidOperationException("A kill switch instance cannot observe more than one receive endpoint.");

            if (_receiveEndpoint is null)
            {
                _receiveEndpoint = endpoint;
                _logContext = endpoint.LogContext;
                connectObserver = true;
            }

            if (_state is KillSwitchState.Initial or KillSwitchState.Terminated)
            {
                if (_state == KillSwitchState.Terminated)
                {
                    previousLifetime = _lifetimeCancellation;
                    _lifetimeCancellation = new CancellationTokenSource();
                }

                ResetWindow();
                _state = KillSwitchState.Running;
            }
        }

        previousLifetime?.Dispose();

        if (connectObserver)
            endpoint.ConnectConsumeObserver(this);
    }

    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        CancellationTokenSource cancellation;
        Task recoveryTask;

        lock (_lock)
        {
            if (_selfStopping || _state == KillSwitchState.Terminated)
                return Task.CompletedTask;

            _state = KillSwitchState.Terminated;
            cancellation = _lifetimeCancellation;
            recoveryTask = _recoveryTask;
        }

        cancellation.Cancel();
        return recoveryTask;
    }

    public Task CompletedAsync(ReceiveEndpointCompleted completed) => Task.CompletedTask;

    public Task FaultedAsync(ReceiveEndpointFaulted faulted) => Task.CompletedTask;

    private void RecordAttempt()
    {
        lock (_lock)
        {
            if (_state is not (KillSwitchState.Running or KillSwitchState.VerifyingRecovery))
                return;

            if (_state == KillSwitchState.Running)
                ResetExpiredWindow();

            _attemptCount++;
        }
    }

    private void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state is not (KillSwitchState.Running or KillSwitchState.VerifyingRecovery))
                return;

            if (_state == KillSwitchState.Running)
            {
                ResetExpiredWindow();
                _successCount++;
                return;
            }

            _successCount++;
            if (_attemptCount >= _settings.ActivationThreshold)
            {
                ResetWindow();
                _state = KillSwitchState.Running;
            }
        }
    }

    private void RecordFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!_settings.ExceptionFilter.Match(exception))
            return;

        IKillSwitchEndpoint? endpoint = null;
        CancellationToken recoveryCancellation = default;
        var attemptCount = 0;
        var failureCount = 0;

        lock (_lock)
        {
            if (_state is not (KillSwitchState.Running or KillSwitchState.VerifyingRecovery))
                return;

            bool verifyingRecovery = _state == KillSwitchState.VerifyingRecovery;
            if (!verifyingRecovery)
                ResetExpiredWindow();

            _failureCount++;

            if (!verifyingRecovery
                && (_attemptCount < _settings.ActivationThreshold
                    || _failureCount / (double)_attemptCount < _settings.TripThresholdRatio))
                return;

            endpoint = _receiveEndpoint
                ?? throw new InvalidOperationException("The kill switch received a delivery observation before its endpoint was ready.");
            recoveryCancellation = _lifetimeCancellation.Token;
            attemptCount = _attemptCount;
            failureCount = _failureCount;
            _state = KillSwitchState.Stopping;

            // RecoverAsync yields before touching the endpoint, so the task is owned and visible before
            // any lifecycle callback can re-enter the switch.
            _recoveryTask = RecoverAsync(endpoint, exception, recoveryCancellation);
        }

        RunInOwnLogContext(() =>
            LogContext.Debug?.Log(
                "Kill switch threshold reached, failures: {FailureCount}, attempts: {AttemptCount}, input address: {InputAddress}",
                failureCount,
                attemptCount,
                endpoint.InputAddress));
    }

    private async Task RecoverAsync(
        IKillSwitchEndpoint endpoint,
        Exception triggeringException,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        SetOwnLogContext();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PauseUntilSuccessfulAsync(endpoint, cancellationToken).ConfigureAwait(false);
                SetState(KillSwitchState.Paused);

                var restartAt = _settings.TimeProvider.GetUtcNow() + _settings.RestartDelay;
                LogContext.Info?.Log(
                    triggeringException,
                    "Kill switch paused endpoint, restarting at {RestartAt}: {InputAddress}",
                    restartAt,
                    endpoint.InputAddress);

                await Task.Delay(_settings.RestartDelay, _settings.TimeProvider, cancellationToken).ConfigureAwait(false);
                SetState(KillSwitchState.Starting);

                try
                {
                    SetOwnLogContext();
                    ReceiveEndpointHandle handle = await endpoint.RestartAsync(cancellationToken).ConfigureAwait(false);
                    await handle.Ready.WaitAsync(cancellationToken).ConfigureAwait(false);
                    SetVerifyingRecovery();
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    SetOwnLogContext();
                    LogContext.Error?.Log(
                        exception,
                        "Kill switch failed to restart endpoint and will retry after pausing it again: {InputAddress}",
                        endpoint.InputAddress);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A terminal endpoint stop owns cancellation. It is an expected lifecycle transition.
        }
    }

    private async Task PauseUntilSuccessfulAsync(
        IKillSwitchEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetState(KillSwitchState.Stopping);

            lock (_lock)
                _selfStopping = true;

            try
            {
                SetOwnLogContext();
                await endpoint.PauseAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                SetOwnLogContext();
                LogContext.Error?.Log(
                    exception,
                    "Kill switch failed to pause endpoint and will retry: {InputAddress}",
                    endpoint.InputAddress);
            }
            finally
            {
                lock (_lock)
                    _selfStopping = false;
            }

            await Task.Delay(_settings.RestartDelay, _settings.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private void SetState(KillSwitchState state)
    {
        lock (_lock)
        {
            if (_state != KillSwitchState.Terminated)
                _state = state;
        }
    }

    private void SetVerifyingRecovery()
    {
        lock (_lock)
        {
            if (_state == KillSwitchState.Terminated)
                return;

            ResetWindow();
            _state = KillSwitchState.VerifyingRecovery;
        }
    }

    private void ResetExpiredWindow()
    {
        if (_settings.TimeProvider.GetElapsedTime(_windowStartedAt) >= _settings.TrackingPeriod)
            ResetWindow();
    }

    private void ResetWindow()
    {
        _attemptCount = 0;
        _successCount = 0;
        _failureCount = 0;
        _windowStartedAt = _settings.TimeProvider.GetTimestamp();
    }

    private void SetOwnLogContext()
    {
        if (_logContext is not null)
            LogContext.Current = _logContext;
    }

    private void RunInOwnLogContext(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ILogContext? previous = LogContext.Current;
        try
        {
            SetOwnLogContext();
            action();
        }
        finally
        {
            LogContext.Current = previous!;
        }
    }
}


internal enum KillSwitchState
{
    /// <summary>
    /// Indicates initial.
    /// </summary>
    Initial,
    /// <summary>
    /// Indicates running.
    /// </summary>
    Running,
    /// <summary>
    /// Indicates stopping.
    /// </summary>
    Stopping,
    /// <summary>
    /// Indicates paused.
    /// </summary>
    Paused,
    /// <summary>
    /// Indicates starting.
    /// </summary>
    Starting,
    /// <summary>
    /// Indicates verifying recovery.
    /// </summary>
    VerifyingRecovery,
    /// <summary>
    /// Indicates terminated.
    /// </summary>
    Terminated
}


internal readonly record struct KillSwitchSnapshot(
    KillSwitchState State,
    int AttemptCount,
    int SuccessCount,
    int FailureCount,
    bool RecoveryActive);
