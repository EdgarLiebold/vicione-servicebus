using System;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides date time tick services.</summary>
public class DateTimeTickProvider :
    ITickProvider
{
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public DateTimeTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the ticks.</summary>
    public long Ticks => _timeProvider.GetUtcNow().UtcDateTime.Ticks;
}
