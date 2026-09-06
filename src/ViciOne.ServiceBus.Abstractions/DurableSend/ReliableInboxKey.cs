namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Identifies one consumer's durable processing of one incoming message.</summary>
/// <param name="MessageId">The message id.</param>
/// <param name="ConsumerId">The consumer id.</param>
public readonly record struct ReliableInboxKey(Guid MessageId, Guid ConsumerId)
{
    /// <summary>Validates that both identity components are non-empty.</summary>
    /// <returns>The validation failures.</returns>
    public ReliableInboxKey Validate()
    {
        if (MessageId == Guid.Empty)
            throw new ArgumentException("The inbox message id cannot be empty.", nameof(MessageId));
        if (ConsumerId == Guid.Empty)
            throw new ArgumentException("The inbox consumer id cannot be empty.", nameof(ConsumerId));
        return this;
    }
}
