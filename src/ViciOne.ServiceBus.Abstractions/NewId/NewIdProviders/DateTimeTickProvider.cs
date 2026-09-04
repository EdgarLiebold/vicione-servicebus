using System;

namespace ViciOne.ServiceBus.NewIdProviders;

public class DateTimeTickProvider :
    ITickProvider
{
    readonly TimeProvider _timeProvider;

    public DateTimeTickProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public long Ticks => _timeProvider.GetUtcNow().UtcDateTime.Ticks;
}
