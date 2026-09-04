using System;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>
/// Provides a stopwatch tick provider implementation.
/// </summary>
public class StopwatchTickProvider :
    ITickProvider
{
    readonly DateTimeOffset _start;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    public StopwatchTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _start = _timeProvider.GetUtcNow();
        _startedAt = _timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Gets the ticks value.
    /// </summary>
    public long Ticks => _start.Add(_timeProvider.GetElapsedTime(_startedAt)).UtcDateTime.Ticks;
}
