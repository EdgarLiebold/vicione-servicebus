namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides partition-key access for send and consume contexts.</summary>
public static class PartitionKeyExtensions
{
    /// <summary>Gets the partition key associated with a received message.</summary>
    /// <param name="context">The consume context.</param>
    /// <returns>The partition key, or <see langword="null" /> when none is available.</returns>
    public static string? PartitionKey(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out PartitionKeyConsumeContext? consumeContext) ? consumeContext.PartitionKey : null;
    }

    /// <summary>Gets the partition key assigned to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <returns>The partition key, or <see langword="null" /> when none is available.</returns>
    public static string? PartitionKey(this SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetPayload(out PartitionKeySendContext? sendContext) ? sendContext.PartitionKey : null;
    }

    /// <summary>Assigns a partition key to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <param name="partitionKey">The partition key, or <see langword="null" /> to clear it.</param>
    /// <exception cref="NotSupportedException">The active transport does not expose partition-key support.</exception>
    public static void SetPartitionKey(this SendContext context, string? partitionKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.TryGetPayload(out PartitionKeySendContext? sendContext))
            throw new NotSupportedException("The active send transport does not support partition keys.");

        sendContext.PartitionKey = partitionKey;
    }

    /// <summary>Tries to assign a partition key to an outgoing message.</summary>
    /// <param name="context">The send context.</param>
    /// <param name="partitionKey">The partition key, or <see langword="null" /> to clear it.</param>
    /// <returns><see langword="true" /> when the active transport accepted the partition key; otherwise, <see langword="false" />.</returns>
    public static bool TrySetPartitionKey(this SendContext context, string? partitionKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.TryGetPayload(out PartitionKeySendContext? sendContext))
            return false;

        sendContext.PartitionKey = partitionKey;
        return true;
    }
}
