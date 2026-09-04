using System;

namespace ViciOne.ServiceBus.QuartzIntegration;

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
