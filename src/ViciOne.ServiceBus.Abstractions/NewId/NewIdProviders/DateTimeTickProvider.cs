using System;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>
/// Provides a date time tick provider implementation.
/// </summary>
public class DateTimeTickProvider :
    ITickProvider
{
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    public DateTimeTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the ticks value.
    /// </summary>
    public long Ticks => _timeProvider.GetUtcNow().UtcDateTime.Ticks;
}
