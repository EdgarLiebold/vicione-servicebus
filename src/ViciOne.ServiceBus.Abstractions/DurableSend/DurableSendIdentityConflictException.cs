using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when a durable-send id is reused for a different immutable send intent.
/// </summary>
public sealed class DurableSendIdentityConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    public DurableSendIdentityConflictException(DurableSendId id)
        : base($"Durable send id '{id}' is already bound to a different send intent.")
        => Id = id;

    /// <summary>
    /// Gets the id value.
    /// </summary>
    public DurableSendId Id { get; }
}
