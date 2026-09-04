using System;

namespace ViciOne.ServiceBus.NewIdProviders;

public class DateTimeTickProvider :
    ITickProvider
{
    public long Ticks => DateTime.UtcNow.Ticks;
}
