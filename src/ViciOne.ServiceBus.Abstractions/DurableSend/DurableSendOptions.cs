using System;

namespace ViciOne.ServiceBus;

/// <summary>Application-owned identity and optional correlation for one typed durable send.</summary>
public sealed record DurableSendOptions
{
    /// <summary>
    /// Stable caller-owned idempotency identity. Reusing it with the same intent is idempotent; reusing it with a
    /// different intent fails closed.
    /// </summary>
    public required DurableSendId IdempotencyKey { get; init; }

    /// <summary>Gets the optional correlation identity copied to the canonical send context before serialization.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Gets the optional first-delivery time used by the reliable scheduler.</summary>
    public DateTimeOffset? DueAt { get; init; }

    internal ScheduleOptions? ScheduledMessageOptions { get; init; }

    internal DurableSendOptions Validate()
    {
        if (IdempotencyKey.Value == Guid.Empty)
            throw new ArgumentException("A durable send requires a non-empty idempotency key.", nameof(IdempotencyKey));
        if (CorrelationId == Guid.Empty)
            throw new ArgumentException("A durable send correlation id cannot be empty.", nameof(CorrelationId));

        return this;
    }
}
