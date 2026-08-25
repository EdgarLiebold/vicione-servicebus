using Microsoft.Extensions.Time.Testing;

namespace ViciOne.ServiceBus.Tests.Testing;

internal sealed class ObservableTimeProvider(DateTimeOffset startTime) : TimeProvider
{
    private readonly FakeTimeProvider _inner = new(startTime);
    private readonly object _lock = new();
    private readonly Dictionary<int, TaskCompletionSource<bool>> _timerWaiters = [];
    private int _activeTimerCount;
    private int _changeCount;
    private int _timerCount;
    private TimeSpan? _lastDueTime;

    public int ActiveTimerCount => Volatile.Read(ref _activeTimerCount);

    public int TimerCount
    {
        get
        {
            lock (_lock)
                return _timerCount;
        }
    }

    public int ChangeCount
    {
        get
        {
            lock (_lock)
                return _changeCount;
        }
    }

    public TimeSpan? LastDueTime
    {
        get
        {
            lock (_lock)
                return _lastDueTime;
        }
    }

    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

    public override long TimestampFrequency => _inner.TimestampFrequency;

    public override long GetTimestamp() => _inner.GetTimestamp();

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ITimer timer = _inner.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _activeTimerCount);

        TaskCompletionSource<bool>[] completedWaiters;
        lock (_lock)
        {
            _timerCount++;
            _lastDueTime = dueTime;
            completedWaiters = _timerWaiters
                .Where(waiter => waiter.Key <= _timerCount)
                .Select(waiter => waiter.Value)
                .ToArray();

            foreach (int completedCount in _timerWaiters.Keys.Where(count => count <= _timerCount).ToArray())
                _timerWaiters.Remove(completedCount);
        }

        foreach (TaskCompletionSource<bool> waiter in completedWaiters)
            waiter.TrySetResult(true);

        return new ObservableTimer(this, timer);
    }

    public void Advance(TimeSpan elapsed) => _inner.Advance(elapsed);

    public Task WaitForTimerCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        lock (_lock)
        {
            if (_timerCount >= count)
                return Task.CompletedTask;

            if (!_timerWaiters.TryGetValue(count, out TaskCompletionSource<bool>? waiter))
            {
                waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _timerWaiters.Add(count, waiter);
            }

            return waiter.Task;
        }
    }

    private void TimerDisposed() => Interlocked.Decrement(ref _activeTimerCount);

    private void TimerChanged(TimeSpan dueTime)
    {
        lock (_lock)
        {
            _changeCount++;
            _lastDueTime = dueTime;
        }
    }

    private sealed class ObservableTimer(ObservableTimeProvider owner, ITimer inner) : ITimer
    {
        private int _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            bool changed = inner.Change(dueTime, period);
            if (changed)
                owner.TimerChanged(dueTime);

            return changed;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                inner.Dispose();
            }
            finally
            {
                owner.TimerDisposed();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                await inner.DisposeAsync();
            }
            finally
            {
                owner.TimerDisposed();
            }
        }
    }
}
