using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides extension methods for sql send context.</summary>
public static class SqlSendContextExtensions
{
    /// <summary>Sets the message priority (default: 100).</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="priority">The priority.</param>
    public static void SetPriority(this SendContext context, short priority)
    {
        if (!context.TryGetPayload(out SqlSendContext? sendContext))
            throw new ArgumentException("The DbSendContext was not available");

        sendContext.Priority = priority == 100 ? default(short?) : priority;
    }

    /// <summary>Sets the message priority (default: 100).</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="priority">The priority.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TrySetPriority(this SendContext context, short priority)
    {
        if (!context.TryGetPayload(out SqlSendContext? sendContext))
            return false;

        sendContext.Priority = priority == 100 ? default(short?) : priority;
        return true;
    }
}
