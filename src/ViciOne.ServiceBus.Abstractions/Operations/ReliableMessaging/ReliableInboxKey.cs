namespace ViciOne.ServiceBus.Operations;

/// <summary>Identifies one consumer's durable processing of one incoming message.</summary>
/// <param name="MessageId">The transport message identity.</param>
/// <param name="ConsumerId">The stable consumer identity.</param>
public readonly record struct ReliableInboxKey(Guid MessageId, Guid ConsumerId)
{
    /// <summary>Validates that both identity components are non-empty.</summary>
    /// <returns>This identity when both components are nonempty.</returns>
    /// <exception cref="ArgumentException">Either identity component is empty.</exception>
    public ReliableInboxKey Validate()
    {
        if (MessageId == Guid.Empty)
            throw new ArgumentException("The inbox message id cannot be empty.", nameof(MessageId));
        if (ConsumerId == Guid.Empty)
            throw new ArgumentException("The inbox consumer id cannot be empty.", nameof(ConsumerId));
        return this;
    }
}
