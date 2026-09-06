using System;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ-native settings on a transport send context.</summary>
public static class ActiveMqSendContextExtensions
{
    /// <summary>Sets the priority of a message sent to the broker.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <exception cref="ArgumentException">The send context does not contain an ActiveMQ payload.</exception>
    public static void SetPriority(this SendContext context, MsgPriority priority)
    {
        if (!context.TryGetPayload(out ActiveMqSendContext? sendContext))
            throw new ArgumentException("The ActiveMqSendContext was not available");

        sendContext.Priority = priority;
    }

    /// <summary>Sets the priority of a message sent to the broker.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="priority">The Apache NMS message priority.</param>
    /// <returns><see langword="true" /> when the context contains an ActiveMQ payload and its priority was set; otherwise, <see langword="false" />.</returns>
    public static bool TrySetPriority(this SendContext context, MsgPriority priority)
    {
        if (!context.TryGetPayload(out ActiveMqSendContext? sendContext))
            return false;

        sendContext.Priority = priority;
        return true;
    }
}
