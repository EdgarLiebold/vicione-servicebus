using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.NewIdProviders;

public class StopwatchTickProvider :
    ITickProvider
{
    readonly DateTime _start;
    readonly Stopwatch _stopwatch;

    public StopwatchTickProvider()
    {
        _start = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
    }

    public long Ticks => _start.AddTicks(_stopwatch.Elapsed.Ticks).Ticks;
}
