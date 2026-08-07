// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public static class ServiceBusMessageContextExtensions
{
    public static string SessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.SessionId : string.Empty;
    }

    public static string ReplyToSessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.ReplyToSessionId : string.Empty;
    }
}
