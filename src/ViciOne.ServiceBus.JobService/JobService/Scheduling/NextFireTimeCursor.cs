using System;

namespace ViciOne.ServiceBus.JobService.Scheduling;

internal readonly struct NextFireTimeCursor(bool restartLoop, DateTimeOffset? date)
{
    public bool RestartLoop { get; } = restartLoop;

    public DateTimeOffset? Date { get; } = date;

    public void Deconstruct(out bool restartLoop, out DateTimeOffset? date)
    {
        restartLoop = RestartLoop;
        date = Date;
    }
}
