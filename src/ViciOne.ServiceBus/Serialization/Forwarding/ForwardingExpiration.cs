using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Serialization;

internal static class ForwardingExpiration
{
    public static bool MarkIfExpired(SendContext context, DateTimeOffset? inheritedExpirationTime, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        TimeSpan? timeToLive = context.TimeToLive;
        bool hasExpiredTimeToLive = timeToLive.HasValue && timeToLive.Value <= TimeSpan.Zero;
        bool hasExpiredInheritedTime = !timeToLive.HasValue
            && inheritedExpirationTime.HasValue
            && inheritedExpirationTime.Value.ToUniversalTime() <= timeProvider.GetUtcNow();

        if (!hasExpiredTimeToLive && !hasExpiredInheritedTime)
            return false;

        context.GetOrAddPayload(() => new ExpiredForwarding(
            inheritedExpirationTime?.ToUniversalTime(),
            timeToLive));
        return true;
    }

    public static bool TryDiscard<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.TryGetPayload(out ExpiredForwarding? expiration))
            return false;

        context.LogExpiredForward(expiration.InheritedExpirationTime, expiration.TimeToLive);
        return true;
    }
}
