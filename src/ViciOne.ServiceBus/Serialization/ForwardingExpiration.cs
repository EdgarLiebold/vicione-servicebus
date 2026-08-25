namespace ViciOne.ServiceBus.Serialization
{
    using System;
    using Transports;


    internal static class ForwardingExpiration
    {
        public static bool MarkIfExpired(SendContext context, DateTime? inheritedExpirationTime)
        {
            TimeSpan? timeToLive = context.TimeToLive;
            bool hasExpiredTimeToLive = timeToLive.HasValue && timeToLive.Value <= TimeSpan.Zero;
            bool hasExpiredInheritedTime = !timeToLive.HasValue
                && inheritedExpirationTime.HasValue
                && inheritedExpirationTime.Value.ToUniversalTime() <= DateTime.UtcNow;

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
            if (!context.TryGetPayload(out ExpiredForwarding expiration))
                return false;

            context.LogExpiredForward(expiration.InheritedExpirationTime, expiration.TimeToLive);
            return true;
        }
    }


    internal sealed record ExpiredForwarding(
        DateTime? InheritedExpirationTime,
        TimeSpan? TimeToLive);
}
