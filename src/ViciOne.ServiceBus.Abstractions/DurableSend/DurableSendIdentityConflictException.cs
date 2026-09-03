using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when a durable-send id is reused for a different immutable send intent.
/// </summary>
public sealed class DurableSendIdentityConflictException : Exception
{
    public DurableSendIdentityConflictException(DurableSendId id)
        : base($"Durable send id '{id}' is already bound to a different send intent.")
        => Id = id;

    public DurableSendId Id { get; }
}
