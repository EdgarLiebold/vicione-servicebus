using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures SQL-specific properties on a message send context.</summary>
public static class SqlSendContextExtensions
{
    /// <summary>Sets the SQL transport priority, where <c>100</c> selects the transport default.</summary>
    /// <param name="context">The send context to configure.</param>
    /// <param name="priority">The database delivery priority.</param>
    public static void SetPriority(this SendContext context, short priority)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGetPayload(out SqlSendContext? sendContext))
            throw new InvalidOperationException("The send context does not contain SQL transport state.");

        sendContext.Priority = priority == 100 ? default(short?) : priority;
    }

    /// <summary>Attempts to set the SQL transport priority, where <c>100</c> selects the transport default.</summary>
    /// <param name="context">The send context to configure.</param>
    /// <param name="priority">The database delivery priority.</param>
    /// <returns><see langword="true" /> when SQL transport state was available; otherwise, <see langword="false" />.</returns>
    public static bool TrySetPriority(this SendContext context, short priority)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGetPayload(out SqlSendContext? sendContext))
            return false;

        sendContext.Priority = priority == 100 ? default(short?) : priority;
        return true;
    }
}
