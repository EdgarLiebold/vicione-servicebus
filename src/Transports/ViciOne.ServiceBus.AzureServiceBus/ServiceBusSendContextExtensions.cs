using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides extension methods for service bus send context.
/// </summary>
public static class ServiceBusSendContextExtensions
{
    /// <summary>
    /// Sets the absolute time at which Azure Service Bus should enqueue the message.
    /// </summary>
    /// <param name="context">The send context to configure.</param>
    /// <param name="dueAt">The scheduled enqueue time.</param>
    public static void SetScheduledEnqueueTime(this SendContext context, DateTimeOffset dueAt)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
        {
            sendContext.ScheduledEnqueueTimeUtc = dueAt.ToUniversalTime();
        }
    }

    /// <summary>
    /// Sets the relative delay before Azure Service Bus should enqueue the message.
    /// </summary>
    /// <param name="context">The send context to configure.</param>
    /// <param name="delay">The duration to wait before enqueueing the message.</param>
    public static void SetScheduledEnqueueTime(this SendContext context, TimeSpan delay)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ScheduledEnqueueTimeUtc = context.GetTimeProvider().GetUtcNow() + delay;
    }

    /// <summary>
    /// Sets session id.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sessionId">The session id value.</param>
    public static void SetSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.SessionId = sessionId;
    }

    /// <summary>
    /// Sets reply to session id.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sessionId">The session id value.</param>
    public static void SetReplyToSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyToSessionId = sessionId;
    }

    /// <summary>
    /// Sets reply to.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="replyTo">The reply to value.</param>
    public static void SetReplyTo(this SendContext context, string replyTo)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyTo = replyTo;
    }

    /// <summary>
    /// Sets label.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="label">The label value.</param>
    public static void SetLabel(this SendContext context, string label)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.Label = label;
    }
}
