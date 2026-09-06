using System;
using System.Threading;

namespace ViciOne.ServiceBus.Middleware.CircuitBreaker;

internal sealed class CircuitBreakerStateMachine
{
    private readonly CircuitBreakerSettings _settings;
    private State _state;

    public CircuitBreakerStateMachine(CircuitBreakerSettings settings)
    {
        _settings = settings;
        _state = new ClosedState(settings.TimeProvider.GetTimestamp());
    }

    public CircuitBreakerLease Acquire()
    {
        while (true)
        {
            State state = Volatile.Read(ref _state);
            switch (state)
            {
                case ClosedState closed:
                    if (_settings.TimeProvider.GetElapsedTime(closed.WindowStartedAt) >= _settings.SamplingDuration)
                    {
                        var freshWindow = new ClosedState(_settings.TimeProvider.GetTimestamp());
                        if (ReferenceEquals(Interlocked.CompareExchange(ref _state, freshWindow, closed), closed))
                            state = freshWindow;
                        else
                            continue;
                    }

                    Interlocked.Increment(ref ((ClosedState)state).AttemptCount);
                    return new CircuitBreakerLease(state);

                case OpenState open:
                    TimeSpan elapsed = _settings.TimeProvider.GetElapsedTime(open.OpenedAt);
                    if (elapsed < open.Duration)
                        throw Reject(open.Duration - elapsed, probeInProgress: false, open.LastFailure);

                    var recovery = new HalfOpenState(open.LastFailure, open.DurationIndex, ownsProbe: true);
                    if (!ReferenceEquals(Interlocked.CompareExchange(ref _state, recovery, open), open))
                        continue;

                    CircuitBreakerTelemetry.StateTransition("open", "half_open");
                    CircuitBreakerTelemetry.ProbeAcquired();
                    return new CircuitBreakerLease(recovery);

                case HalfOpenState halfOpen:
                    if (!halfOpen.TryAcquireProbe())
                        throw Reject(TimeSpan.Zero, probeInProgress: true, halfOpen.LastFailure);

                    CircuitBreakerTelemetry.ProbeAcquired();
                    return new CircuitBreakerLease(halfOpen);

                default:
                    throw new InvalidOperationException($"Unknown circuit-breaker state {state.GetType().FullName}.");
            }
        }
    }

    public void RecordSuccess(CircuitBreakerLease lease)
    {
        if (lease.State is not HalfOpenState halfOpen)
            return;

        var closed = new ClosedState(_settings.TimeProvider.GetTimestamp());
        if (ReferenceEquals(Interlocked.CompareExchange(ref _state, closed, halfOpen), halfOpen))
            CircuitBreakerTelemetry.StateTransition("half_open", "closed");
    }

    public void RecordFailure(CircuitBreakerLease lease, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        switch (lease.State)
        {
            case ClosedState closed:
                ObserveClosedFailure(closed, exception);
                break;

            case HalfOpenState halfOpen:
                OpenFromHalfOpen(halfOpen, exception);
                break;
        }
    }

    public void ReleaseWithoutVerdict(CircuitBreakerLease lease)
    {
        if (lease.State is HalfOpenState halfOpen && ReferenceEquals(Volatile.Read(ref _state), halfOpen))
            halfOpen.ReleaseProbe();
    }

    public CircuitBreakerSnapshot GetSnapshot()
    {
        State state = Volatile.Read(ref _state);
        return state switch
        {
            ClosedState closed => new CircuitBreakerSnapshot(
                "closed",
                Volatile.Read(ref closed.AttemptCount),
                Volatile.Read(ref closed.FailureCount),
                TimeSpan.Zero,
                false),
            OpenState open => new CircuitBreakerSnapshot(
                "open",
                0,
                0,
                Remaining(open),
                false),
            HalfOpenState halfOpen => new CircuitBreakerSnapshot(
                "half_open",
                0,
                0,
                TimeSpan.Zero,
                halfOpen.ProbeInProgress),
            _ => throw new InvalidOperationException($"Unknown circuit-breaker state {state.GetType().FullName}."),
        };
    }

    private void ObserveClosedFailure(ClosedState closed, Exception exception)
    {
        if (!ReferenceEquals(Volatile.Read(ref _state), closed))
            return;

        int failures = Interlocked.Increment(ref closed.FailureCount);
        int attempts = Volatile.Read(ref closed.AttemptCount);
        if (attempts < _settings.MinimumThroughput || failures / (double)attempts < _settings.FailureRatio)
            return;

        var open = CreateOpen(exception, durationIndex: 0);
        if (ReferenceEquals(Interlocked.CompareExchange(ref _state, open, closed), closed))
            CircuitBreakerTelemetry.StateTransition("closed", "open");
    }

    private void OpenFromHalfOpen(HalfOpenState halfOpen, Exception exception)
    {
        int durationIndex = Math.Min(halfOpen.DurationIndex + 1, _settings.BreakDurations.Length - 1);
        var open = CreateOpen(exception, durationIndex);
        if (ReferenceEquals(Interlocked.CompareExchange(ref _state, open, halfOpen), halfOpen))
            CircuitBreakerTelemetry.StateTransition("half_open", "open");
    }

    private OpenState CreateOpen(Exception exception, int durationIndex) => new(
        exception,
        _settings.TimeProvider.GetTimestamp(),
        _settings.BreakDurations[durationIndex],
        durationIndex);

    private CircuitBreakerOpenException Reject(TimeSpan retryAfter, bool probeInProgress, Exception lastFailure)
    {
        CircuitBreakerTelemetry.Rejected(probeInProgress);
        return new CircuitBreakerOpenException(retryAfter, probeInProgress, lastFailure);
    }

    private TimeSpan Remaining(OpenState open)
    {
        TimeSpan remaining = open.Duration - _settings.TimeProvider.GetElapsedTime(open.OpenedAt);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    internal abstract class State;

    internal sealed class ClosedState(long windowStartedAt) : State
    {
        public long WindowStartedAt { get; } = windowStartedAt;
        public int AttemptCount;
        public int FailureCount;
    }

    internal sealed class OpenState(
        Exception lastFailure,
        long openedAt,
        TimeSpan duration,
        int durationIndex) : State
    {
        public Exception LastFailure { get; } = lastFailure;
        public long OpenedAt { get; } = openedAt;
        public TimeSpan Duration { get; } = duration;
        public int DurationIndex { get; } = durationIndex;
    }

    internal sealed class HalfOpenState(Exception lastFailure, int durationIndex, bool ownsProbe) : State
    {
        private int _probeInProgress = ownsProbe ? 1 : 0;

        public Exception LastFailure { get; } = lastFailure;
        public int DurationIndex { get; } = durationIndex;
        public bool ProbeInProgress => Volatile.Read(ref _probeInProgress) == 1;

        public bool TryAcquireProbe() => Interlocked.CompareExchange(ref _probeInProgress, 1, 0) == 0;

        public void ReleaseProbe() => Volatile.Write(ref _probeInProgress, 0);
    }
}

internal readonly record struct CircuitBreakerLease(CircuitBreakerStateMachine.State State);

internal readonly record struct CircuitBreakerSnapshot(
    string State,
    int AttemptCount,
    int FailureCount,
    TimeSpan RetryAfter,
    bool ProbeInProgress);
