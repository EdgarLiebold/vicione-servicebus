using System.Collections.Concurrent;

namespace BatchClock;

// Wall time is deliberately independent of the relative timer/timestamp scheduler.
public sealed class Clock : TimeProvider
{
    readonly ConcurrentQueue<ManualTimer> _timers = new();
    long _utcTicks = Epoch.UtcTicks;
    long _elapsed;
    int _reads;
    public static DateTimeOffset Epoch { get; } = new(2040, 1, 2, 0, 0, 0, TimeSpan.Zero);
    public readonly ConcurrentQueue<(long Start, TimeSpan Due, long Deadline)> Schedules = new();
    readonly ConcurrentDictionary<int, TaskCompletionSource> _readSignals = new();
    readonly ConcurrentDictionary<int, TaskCompletionSource> _scheduleSignals = new();
    readonly ConcurrentDictionary<int, TaskCompletionSource> _disposeSignals = new();
    public int Creates, Disposals, Fires, Stops;
    public Exception? ScheduleFailure, StopFailure, DisposeFailure;
    public int FailScheduleAt;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Volatile.Read(ref _elapsed);
    public void SetUtc(DateTimeOffset utc) => Interlocked.Exchange(ref _utcTicks, utc.UtcTicks);
    public override DateTimeOffset GetUtcNow()
    {
        var result = new DateTimeOffset(Volatile.Read(ref _utcTicks), TimeSpan.Zero);
        Signal(_readSignals, Interlocked.Increment(ref _reads));
        return result;
    }
    public Task Read(int count) => GetSignal(_readSignals, count).Task;
    public Task Scheduled(int count) => GetSignal(_scheduleSignals, count).Task;
    public Task Disposed(int count) => GetSignal(_disposeSignals, count).Task;
    static TaskCompletionSource GetSignal(ConcurrentDictionary<int, TaskCompletionSource> signals, int count)
        => signals.GetOrAdd(count, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
    static void Signal(ConcurrentDictionary<int, TaskCompletionSource> signals, int count) => GetSignal(signals, count).TrySetResult();
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new ManualTimer(this, callback, state, ExecutionContext.Capture());
        Interlocked.Increment(ref Creates); _timers.Enqueue(timer);
        timer.Change(dueTime, period);
        return timer;
    }
    public void Advance(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        Interlocked.Add(ref _elapsed, elapsed.Ticks);
        foreach (var timer in _timers) timer.FireIfDue();
    }
    public void ReleaseFaults() { ScheduleFailure = StopFailure = DisposeFailure = null; }
    sealed class ManualTimer(Clock owner, TimerCallback callback, object? state, ExecutionContext? execution) : ITimer
    {
        readonly object _gate = new();
        long? _deadline, _period;
        bool _disposed;
        int _finiteChanges;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime < TimeSpan.Zero && dueTime != Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (period < TimeSpan.Zero && period != Timeout.InfiniteTimeSpan) throw new ArgumentOutOfRangeException(nameof(period));
            if (dueTime != Timeout.InfiniteTimeSpan && ++_finiteChanges == owner.FailScheduleAt && owner.ScheduleFailure is { } failure) throw failure;
            lock (_gate)
            {
                if (_disposed) return false;
                _deadline = dueTime == Timeout.InfiniteTimeSpan ? null : checked(owner.GetTimestamp() + dueTime.Ticks);
                _period = period > TimeSpan.Zero ? period.Ticks : null;
            }
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                Interlocked.Increment(ref owner.Stops);
                if (owner.StopFailure is { } stopFailure) throw stopFailure;
            }
            else
            {
                long start = owner.GetTimestamp();
                owner.Schedules.Enqueue((start, dueTime, checked(start + dueTime.Ticks)));
                Signal(owner._scheduleSignals, owner.Schedules.Count);
            }
            return true;
        }
        public void FireIfDue()
        {
            lock (_gate)
            {
                if (_disposed || !_deadline.HasValue || _deadline > owner.GetTimestamp()) return;
                _deadline = _period.HasValue ? checked(owner.GetTimestamp() + _period.Value) : null;
            }
            Interlocked.Increment(ref owner.Fires);
            if (execution == null) callback(state);
            else ExecutionContext.Run(execution.CreateCopy(), _ => callback(state), null);
        }
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true; _deadline = null;
            }
            Signal(owner._disposeSignals, Interlocked.Increment(ref owner.Disposals));
            if (owner.DisposeFailure is { } failure) throw failure;
        }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
