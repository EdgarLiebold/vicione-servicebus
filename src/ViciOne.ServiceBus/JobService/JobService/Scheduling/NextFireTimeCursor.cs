// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.JobService.Scheduling;

using System;


readonly struct NextFireTimeCursor
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
