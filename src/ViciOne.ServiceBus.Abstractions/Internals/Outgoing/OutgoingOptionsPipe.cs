namespace ViciOne.ServiceBus.Internals.Outgoing;

/// <summary>Applies immutable application options to an outgoing transport context.</summary>
internal static class OutgoingOptionsPipe
{
    public static void Apply(SendContext context, OutgoingOptionsSnapshot options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        PartitionKeySendContext? partitionContext = null;
        if (options.PartitionKey is not null && !context.TryGetPayload(out partitionContext))
            throw new NotSupportedException("The selected transport does not support partition keys.");

        foreach ((string key, object? value) in options.Headers)
            context.Headers.Set(key, value);

        if (options.TimeToLive.HasValue)
            context.TimeToLive = options.TimeToLive;
        if (options.CorrelationId.HasValue)
            context.CorrelationId = options.CorrelationId;
        if (options.ConversationId.HasValue)
            context.ConversationId = options.ConversationId;
        if (options.MessageId.HasValue)
            context.MessageId = options.MessageId;
        if (options.RequestId.HasValue)
            context.RequestId = options.RequestId;
        if (partitionContext is not null)
            partitionContext.PartitionKey = options.PartitionKey;
    }
}
