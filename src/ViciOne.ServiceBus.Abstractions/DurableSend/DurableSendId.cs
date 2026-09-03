using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Stable idempotency identity of one producer-side durable send intent.
/// </summary>
public readonly record struct DurableSendId
{
    public DurableSendId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A durable send id cannot be empty.", nameof(value));

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => Value.ToString("D");
}
