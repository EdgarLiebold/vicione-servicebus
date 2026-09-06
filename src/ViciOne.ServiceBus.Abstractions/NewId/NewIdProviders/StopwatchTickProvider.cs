using System;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides stopwatch tick services.</summary>
public class StopwatchTickProvider :
    ITickProvider
{
    readonly DateTimeOffset _start;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public StopwatchTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _start = _timeProvider.GetUtcNow();
        _startedAt = _timeProvider.GetTimestamp();
    }

    /// <summary>Gets the ticks.</summary>
    public long Ticks => _start.Add(_timeProvider.GetElapsedTime(_startedAt)).UtcDateTime.Ticks;
}
