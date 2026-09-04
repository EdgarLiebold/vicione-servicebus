using System;

namespace ViciOne.ServiceBus;

public static class ServiceBusSendContextExtensions
{
    /// <summary>
    /// Set the time at which the message should be delivered to the queue
    /// </summary>
    /// <param name="context"></param>
    /// <param name="dueAt">The scheduled time for the message</param>
    public static void SetScheduledEnqueueTime(this SendContext context, DateTimeOffset dueAt)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
        {
            sendContext.ScheduledEnqueueTimeUtc = dueAt.ToUniversalTime();
        }
    }

    /// <summary>
    /// Set the time at which the message should be delivered to the queue
    /// </summary>
    /// <param name="context"></param>
    /// <param name="delay">The time to wait before the message should be enqueued</param>
    public static void SetScheduledEnqueueTime(this SendContext context, TimeSpan delay)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ScheduledEnqueueTimeUtc = context.GetTimeProvider().GetUtcNow() + delay;
    }

    public static void SetSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.SessionId = sessionId;
    }

    public static void SetReplyToSessionId(this SendContext context, string sessionId)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyToSessionId = sessionId;
    }

    public static void SetReplyTo(this SendContext context, string replyTo)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.ReplyTo = replyTo;
    }

    public static void SetLabel(this SendContext context, string label)
    {
        if (context.TryGetPayload(out ServiceBusSendContext? sendContext))
            sendContext.Label = label;
    }
}
