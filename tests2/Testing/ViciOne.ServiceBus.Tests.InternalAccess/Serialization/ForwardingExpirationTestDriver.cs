using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Serialization;

public static class ForwardingExpirationTestDriver
{
    public static bool MarkIfExpired(
        SendContext context,
        DateTime? inheritedExpirationTime,
        TimeProvider timeProvider) =>
        ForwardingExpiration.MarkIfExpired(context, inheritedExpirationTime, timeProvider);

    public static bool IsMarkedExpired<T>(SendContext<T> context)
        where T : class =>
        context.TryGetPayload(out ExpiredForwarding? _);
}
