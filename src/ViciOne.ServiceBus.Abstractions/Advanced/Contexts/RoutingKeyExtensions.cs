namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides routing-key access for send and consume contexts.</summary>
public static class RoutingKeyExtensions
{
    /// <summary>Gets the routing key associated with a received message.</summary>
    /// <param name="context">The consume context.</param>
    /// <returns>The routing key, or <see langword="null" /> when none is available.</returns>
    public static string? RoutingKey(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out RoutingKeyConsumeContext? consumeContext) ? consumeContext.RoutingKey : null;
    }

    /// <summary>Gets the routing key assigned to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <returns>The routing key, or <see langword="null" /> when none is available.</returns>
    public static string? RoutingKey(this SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out RoutingKeySendContext? sendContext) ? sendContext.RoutingKey : null;
    }

    /// <summary>Assigns a routing key to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <param name="routingKey">The routing key, or <see langword="null" /> to clear it.</param>
    /// <exception cref="NotSupportedException">The active transport does not expose routing-key support.</exception>
    public static void SetRoutingKey(this SendContext context, string? routingKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.TryGetPayload(out RoutingKeySendContext? sendContext))
            throw new NotSupportedException("The active send transport does not support routing keys.");

        sendContext.RoutingKey = routingKey;
    }

    /// <summary>Tries to assign a routing key to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <param name="routingKey">The routing key, or <see langword="null" /> to clear it.</param>
    /// <returns><see langword="true" /> when the active transport accepted the routing key; otherwise, <see langword="false" />.</returns>
    public static bool TrySetRoutingKey(this SendContext context, string? routingKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.TryGetPayload(out RoutingKeySendContext? sendContext))
            return false;

        sendContext.RoutingKey = routingKey;
        return true;
    }
}
