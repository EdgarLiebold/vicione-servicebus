namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Reads Azure Service Bus session metadata from consume contexts.</summary>
public static class ServiceBusMessageContextExtensions
{
    /// <summary>Gets the message session identifier when Azure Service Bus metadata is available.</summary>
    /// <param name="context">The consume context to inspect.</param>
    /// <returns>The session identifier, or an empty string for a context without Azure Service Bus metadata.</returns>
    public static string SessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.SessionId : string.Empty;
    }

    /// <summary>Gets the reply session identifier when Azure Service Bus metadata is available.</summary>
    /// <param name="context">The consume context to inspect.</param>
    /// <returns>The reply session identifier, or an empty string for a context without Azure Service Bus metadata.</returns>
    public static string ReplyToSessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.ReplyToSessionId : string.Empty;
    }
}
