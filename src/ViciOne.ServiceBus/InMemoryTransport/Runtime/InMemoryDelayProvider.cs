using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Schedules in-memory transport delays against a shared logical clock and timer.</summary>
internal sealed class InMemoryDelayProvider :
    IAsyncDisposable,
    IInMemoryDelayProvider
{
    static readonly TimeSpan MaximumTimerInterval = TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

    readonly SortedSet<ScheduledDelay> _delays = new(ScheduledDelayComparer.Instance);
    readonly object _lock = new();
    readonly CancellationTokenSource _stopping = new();
    readonly TimeProvider _timeProvider;
    readonly ITimer _timer;
    bool _disposed;
    TimeSpan _offset;
    long _sequence;

    /// <summary>Creates a delay provider backed by <see cref="TimeProvider.System" />.</summary>
    public InMemoryDelayProvider()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Creates a delay provider backed by a supplied time source.</summary>
    /// <param name="timeProvider">The source of UTC time and timers.</param>
    public InMemoryDelayProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _timer = timeProvider.CreateTimer(
            static state => ((InMemoryDelayProvider)state!).TimerElapsed(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>Gets the current logical UTC time.</summary>
    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return GetUtcNow();
            }
        }
    }

    /// <summary>Returns a task that completes after a relative logical-time delay.</summary>
    /// <param name="delay">The non-negative delay duration.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The delay cannot be negative.");

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            ThrowIfDisposed();

            if (delay == TimeSpan.Zero)
                return Task.CompletedTask;

            DateTimeOffset deadline;
            try
            {
                deadline = GetUtcNow().Add(delay);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), delay, "The delay exceeds the supported time range.");
            }

            return ScheduleAsync(deadline, cancellationToken);
        }
    }

    /// <summary>Returns a task that completes at an absolute logical-time deadline.</summary>
    /// <param name="delayUntil">The UTC deadline.</param>
    /// <param name="cancellationToken">The token that cancels the pending delay.</param>
    /// <returns>A task that completes when the deadline is reached.</returns>
    public Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            ThrowIfDisposed();

            if (delayUntil <= GetUtcNow())
                return Task.CompletedTask;

            return ScheduleAsync(delayUntil, cancellationToken);
        }
    }

    /// <summary>Advances logical time and releases every delay whose deadline is reached.</summary>
    /// <param name="duration">The positive duration by which logical time advances.</param>
    public void Advance(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "The duration must be greater than zero.");

        List<ScheduledDelay> due;
        lock (_lock)
        {
            ThrowIfDisposed();

            try
            {
                _ = GetUtcNow().Add(duration);
                _offset += duration;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "The duration exceeds the supported time range.");
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "The duration exceeds the supported time range.");
            }

            due = RemoveDueDelays();
            RearmTimer();
        }

        Complete(due);
    }

    /// <summary>Cancels pending delays and releases the shared timer.</summary>
    /// <returns>A value task that completes after every pending delay is canceled and the shared timer is released.</returns>
    public async ValueTask DisposeAsync()
    {
        ScheduledDelay[] pending;
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            pending = [.. _delays];
            _delays.Clear();
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        _stopping.Cancel();
        await _timer.DisposeAsync().ConfigureAwait(false);

        foreach (ScheduledDelay delay in pending)
            delay.Cancel(_stopping.Token);

        _stopping.Dispose();
    }

    Task ScheduleAsync(DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var delay = new ScheduledDelay(this, deadline, ++_sequence, cancellationToken);
        _delays.Add(delay);
        RearmTimer();
        delay.RegisterCancellation();
        return delay.Task;
    }

    void Cancel(ScheduledDelay delay, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_delays.Remove(delay))
                return;

            RearmTimer();
        }

        delay.Cancel(cancellationToken);
    }

    void TimerElapsed()
    {
        List<ScheduledDelay> due;
        lock (_lock)
        {
            if (_disposed)
                return;

            due = RemoveDueDelays();
            RearmTimer();
        }

        Complete(due);
    }

    List<ScheduledDelay> RemoveDueDelays()
    {
        var due = new List<ScheduledDelay>();
        DateTimeOffset now = GetUtcNow();
        while (_delays.Min is { } next && next.Deadline <= now)
        {
            _delays.Remove(next);
            due.Add(next);
        }

        return due;
    }

    void RearmTimer()
    {
        if (_delays.Min is not { } next)
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        TimeSpan dueTime = next.Deadline - GetUtcNow();
        if (dueTime < TimeSpan.Zero)
            dueTime = TimeSpan.Zero;
        else if (dueTime > MaximumTimerInterval)
            dueTime = MaximumTimerInterval;

        _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }

    DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow().Add(_offset);

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    static void Complete(IEnumerable<ScheduledDelay> delays)
    {
        foreach (ScheduledDelay delay in delays)
            delay.Complete();
    }

    sealed class ScheduledDelay(
        InMemoryDelayProvider owner,
        DateTimeOffset deadline,
        long sequence,
        CancellationToken cancellationToken)
    {
        readonly CancellationToken _cancellationToken = cancellationToken;
        readonly InMemoryDelayProvider _owner = owner;
        readonly object _registrationLock = new();
        readonly TaskCompletionSource _source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration _registration;
        bool _completed;
        bool _registrationAssigned;

        public DateTimeOffset Deadline { get; } = deadline;
        public long Sequence { get; } = sequence;
        public Task Task => _source.Task;

        public void RegisterCancellation()
        {
            if (!_cancellationToken.CanBeCanceled)
                return;

            CancellationTokenRegistration registration = _cancellationToken.UnsafeRegister(
                static state => ((ScheduledDelay)state!).CancelFromToken(),
                this);

            var unregister = false;
            lock (_registrationLock)
            {
                if (_completed)
                    unregister = true;
                else
                {
                    _registration = registration;
                    _registrationAssigned = true;
                }
            }

            if (unregister)
                registration.Unregister();
        }

        public void Complete()
        {
            _source.TrySetResult();
            UnregisterCancellation();
        }

        public void Cancel(CancellationToken cancellationToken)
        {
            _source.TrySetCanceled(cancellationToken);
            UnregisterCancellation();
        }

        void CancelFromToken()
        {
            _owner.Cancel(this, _cancellationToken);
        }

        void UnregisterCancellation()
        {
            CancellationTokenRegistration registration = default;
            lock (_registrationLock)
            {
                _completed = true;
                if (_registrationAssigned)
                {
                    registration = _registration;
                    _registrationAssigned = false;
                }
            }

            registration.Unregister();
        }
    }

    sealed class ScheduledDelayComparer : IComparer<ScheduledDelay>
    {
        public static ScheduledDelayComparer Instance { get; } = new();

        public int Compare(ScheduledDelay? x, ScheduledDelay? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            int deadline = x.Deadline.CompareTo(y.Deadline);
            return deadline != 0 ? deadline : x.Sequence.CompareTo(y.Sequence);
        }
    }
}
