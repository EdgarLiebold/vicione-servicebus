namespace ViciOne.ServiceBus.Tests.Testing;

internal sealed class QueuedCallbackTimeProvider(DateTimeOffset startTime, Exception? timerCreationException = null) : TimeProvider
{
    private readonly ObservableTimeProvider _inner = new(startTime);
    private TimerCallback? _callback;
    private object? _state;

    public int ActiveTimerCount => _inner.ActiveTimerCount;
    public int TimerCount => _inner.TimerCount;
    public TimeSpan? LastDueTime => _inner.LastDueTime;

    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();
    public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;
    public override long TimestampFrequency => _inner.TimestampFrequency;
    public override long GetTimestamp() => _inner.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        if (timerCreationException != null)
            throw timerCreationException;

        _callback = callback;
        _state = state;
        return _inner.CreateTimer(callback, state, dueTime, period);
    }

    public void Advance(TimeSpan elapsed) => _inner.Advance(elapsed);

    public Action CaptureQueuedCallback()
    {
        TimerCallback callback = _callback ?? throw new InvalidOperationException("No timer callback has been registered.");
        object? state = _state;
        return () => callback(state);
    }
}
