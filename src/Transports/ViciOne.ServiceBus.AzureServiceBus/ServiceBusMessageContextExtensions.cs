namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides extension methods for service bus message context.
/// </summary>
public static class ServiceBusMessageContextExtensions
{
    /// <summary>
    /// Performs the session id operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static string SessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.SessionId : string.Empty;
    }

    /// <summary>
    /// Performs the reply to session id operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public static string ReplyToSessionId(this ConsumeContext context)
    {
        return context.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext) ? brokeredMessageContext.ReplyToSessionId : string.Empty;
    }
}
