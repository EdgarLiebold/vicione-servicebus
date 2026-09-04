using System;

namespace ViciOne.ServiceBus.Quartz;

internal static class ScheduledMessageExpiration
{
    public static TimeSpan? GetRemainingTimeToLive(DateTimeOffset? expirationTime, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return expirationTime.HasValue
            ? expirationTime.Value.ToUniversalTime() - timeProvider.GetUtcNow()
            : null;
    }
}
