namespace ViciOne.ServiceBus.Scheduling;

internal static class RelativeScheduleTime
{
    internal static DateTimeOffset GetDueAt(IMessageScheduler scheduler, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The scheduling delay cannot be negative.");

        return GetDueAt(scheduler.TimeProvider, delay);
    }

    internal static DateTimeOffset GetDueAt(TimeProvider timeProvider, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The scheduling delay cannot be negative.");

        try
        {
            return timeProvider.GetUtcNow().Add(delay);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The scheduling delay produces a due time outside the supported range.");
        }
    }
}
