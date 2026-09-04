using System;

namespace ViciOne.ServiceBus.NewIdProviders;

public class StopwatchTickProvider :
    ITickProvider
{
    readonly DateTimeOffset _start;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;

    public StopwatchTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _start = _timeProvider.GetUtcNow();
        _startedAt = _timeProvider.GetTimestamp();
    }

    public long Ticks => _start.Add(_timeProvider.GetElapsedTime(_startedAt)).UtcDateTime.Ticks;
}
