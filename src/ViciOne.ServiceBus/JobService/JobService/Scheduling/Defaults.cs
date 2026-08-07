// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.JobService.Scheduling;

using System;


static class Defaults
{
    internal const int FirstYear = 1970;

    internal static readonly int LastYear = DateTime.UtcNow.Year + 100;
}
