using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Defines configuration options for outbox consume.</summary>
public sealed class OutboxConsumeOptions
{
    /// <summary>The generated identifier for the consumer based upon endpoint name.</summary>
    public required Guid ConsumerId { get; init; }

    /// <summary>The display name of the consumer type.</summary>
    public required string ConsumerType { get; init; }
    /// <summary>The number of message to deliver at a time from the outbox.</summary>
    public required int MessageDeliveryLimit { get; init; }

    /// <summary>The time to wait when delivering a message to the broker.</summary>
    public required TimeSpan MessageDeliveryTimeout { get; init; }

    internal void Validate()
    {
        if (ConsumerId == Guid.Empty)
            throw Invalid(nameof(ConsumerId), "must not be empty", "Supply the generated consumer identity");
        if (string.IsNullOrWhiteSpace(ConsumerType))
            throw Invalid(nameof(ConsumerType), "must not be empty", "Supply the consumer display name");
        if (MessageDeliveryLimit <= 0)
            throw Invalid(nameof(MessageDeliveryLimit), "must be greater than zero", "Set a positive delivery batch limit");
        if (MessageDeliveryTimeout <= TimeSpan.Zero)
            throw Invalid(nameof(MessageDeliveryTimeout), "must be greater than zero", "Set a positive broker delivery timeout");
    }

    static ConfigurationException Invalid(string property, string problem, string fix) =>
        new($"In-memory outbox for bus 'default': {property} {problem}. {fix}.");
}
