using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Sets Azure Service Bus-specific properties when the send context supports them.</summary>
public static class ServiceBusSendContextExtensions
{
    /// <summary>Sets the absolute time at which Azure Service Bus should enqueue the message.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="dueAt">The requested enqueue instant.</param>
    public static void SetScheduledEnqueueTime(this SendContext context, DateTimeOffset dueAt)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
        {
            sendContext.ScheduledEnqueueTimeUtc = dueAt.ToUniversalTime();
        }
    }

    /// <summary>Sets the relative delay before Azure Service Bus should enqueue the message.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="delay">The duration to wait before enqueueing the message.</param>
    public static void SetScheduledEnqueueTime(this SendContext context, TimeSpan delay)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ScheduledEnqueueTimeUtc = context.GetTimeProvider().GetUtcNow() + delay;
    }

    /// <summary>Sets the message session identifier and matching partition key.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="sessionId">The session identifier.</param>
    public static void SetSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.SessionId = sessionId;
    }

    /// <summary>Sets the session identifier expected on replies.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="sessionId">The reply session identifier.</param>
    public static void SetReplyToSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyToSessionId = sessionId;
    }

    /// <summary>Sets the reply destination entity path.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="replyTo">The reply destination entity path.</param>
    public static void SetReplyTo(this SendContext context, string replyTo)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyTo = replyTo;
    }

    /// <summary>Sets the application-specific subject label.</summary>
    /// <param name="context">The outgoing message context.</param>
    /// <param name="label">The subject label.</param>
    public static void SetLabel(this SendContext context, string label)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.Label = label;
    }
}
