using System;

namespace ViciOne.ServiceBus;

/// <summary>Stable idempotency identity of one producer-side durable send intent.</summary>
public readonly record struct DurableSendId
{
    /// <summary>Initializes an idempotency identity from a nonempty UUID.</summary>
    /// <param name="value">The durable-send identity.</param>
    public DurableSendId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A durable send id cannot be empty.", nameof(value));

        Value = value;
    }

    /// <summary>Gets the durable-send UUID.</summary>
    public Guid Value { get; }

    /// <summary>Formats the UUID using its canonical lowercase-independent <c>D</c> representation.</summary>
    /// <returns>The canonical UUID representation.</returns>
    public override string ToString() => Value.ToString("D");
}
