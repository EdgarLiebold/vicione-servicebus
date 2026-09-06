using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for routing key.</summary>
public static class RoutingKeyExtensions
{
    /// <summary>Configures the routing key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public static string? RoutingKey(this ConsumeContext context)
    {
        return context.TryGetPayload(out RoutingKeyConsumeContext? consumeContext) ? consumeContext.RoutingKey : string.Empty;
    }

    /// <summary>Configures the routing key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public static string? RoutingKey(this SendContext context)
    {
        return context.TryGetPayload(out RoutingKeySendContext? sendContext) ? sendContext.RoutingKey : string.Empty;
    }

    /// <summary>Sets the routing key for this message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="routingKey">The routing key for this message.</param>
    public static void SetRoutingKey(this SendContext context, string? routingKey)
    {
        if (!context.TryGetPayload(out RoutingKeySendContext? sendContext))
            throw new ArgumentException("The SendRoutingKeyContext was not available");

        sendContext.RoutingKey = routingKey;
    }

    /// <summary>Sets the routing key for this message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="routingKey">The routing key for this message.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TrySetRoutingKey(this SendContext context, string? routingKey)
    {
        if (!context.TryGetPayload(out RoutingKeySendContext? sendContext))
            return false;

        sendContext.RoutingKey = routingKey;
        return true;
    }
}
