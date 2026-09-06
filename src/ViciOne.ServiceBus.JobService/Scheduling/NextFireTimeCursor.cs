using System;

namespace ViciOne.ServiceBus.JobService.Scheduling;

internal readonly struct NextFireTimeCursor
{
    public NextFireTimeCursor(bool restartLoop, DateTimeOffset? date)
    {
        RestartLoop = restartLoop;
        Date = date;
    }

    public bool RestartLoop { get; }

    public DateTimeOffset? Date { get; }

    public void Deconstruct(out bool restartLoop, out DateTimeOffset? date)
    {
        restartLoop = RestartLoop;
        date = Date;
    }
}
