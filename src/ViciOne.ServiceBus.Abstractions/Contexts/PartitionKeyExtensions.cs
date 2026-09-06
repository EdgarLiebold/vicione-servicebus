using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for partition key.</summary>
public static class PartitionKeyExtensions
{
    /// <summary>Configures the partition key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public static string? PartitionKey(this ConsumeContext context)
    {
        return context.TryGetPayload(out PartitionKeyConsumeContext? consumeContext) ? consumeContext.PartitionKey : string.Empty;
    }

    /// <summary>Configures the partition key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public static string? PartitionKey(this SendContext context)
    {
        return context.TryGetPayload(out PartitionKeySendContext? sendContext) ? sendContext.PartitionKey : string.Empty;
    }

    /// <summary>Sets the routing key for this message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="routingKey">The routing key for this message.</param>
    public static void SetPartitionKey(this SendContext context, string? routingKey)
    {
        if (!context.TryGetPayload(out PartitionKeySendContext? sendContext))
            throw new ArgumentException("The SendPartitionKeyContext was not available");

        sendContext.PartitionKey = routingKey;
    }

    /// <summary>Sets the routing key for this message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="routingKey">The routing key for this message.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TrySetPartitionKey(this SendContext context, string? routingKey)
    {
        if (!context.TryGetPayload(out PartitionKeySendContext? sendContext))
            return false;

        sendContext.PartitionKey = routingKey;
        return true;
    }
}
