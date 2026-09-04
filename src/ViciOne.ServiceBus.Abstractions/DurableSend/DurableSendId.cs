using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Stable idempotency identity of one producer-side durable send intent.
/// </summary>
public readonly record struct DurableSendId
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    public DurableSendId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A durable send id cannot be empty.", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString() => Value.ToString("D");
}
