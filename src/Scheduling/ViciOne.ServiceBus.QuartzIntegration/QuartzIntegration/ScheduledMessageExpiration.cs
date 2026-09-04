using System;

namespace ViciOne.ServiceBus.QuartzIntegration;

internal static class ScheduledMessageExpiration
{
    public static TimeSpan? GetRemainingTimeToLive(DateTime? expirationTime, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return expirationTime.HasValue
            ? expirationTime.Value.ToUniversalTime() - timeProvider.GetUtcNow().UtcDateTime
            : null;
    }
}
