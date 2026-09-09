using System;

namespace ViciOne.ServiceBus;

/// <summary>Raised when a durable-send id is reused for a different immutable send intent.</summary>
public sealed class DurableSendIdentityConflictException : ViciOneServiceBusException
{
    /// <summary>Creates a conflict for a previously admitted durable-send identity.</summary>
    /// <param name="id">The identity bound to a different immutable intent.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> is empty.</exception>
    public DurableSendIdentityConflictException(DurableSendId id)
        : base($"Durable send id '{id}' is already bound to a different send intent.")
    {
        if (id.Value == Guid.Empty)
            throw new ArgumentException("A conflicting durable send id cannot be empty.", nameof(id));

        Id = id;
    }

    /// <summary>Gets the identity that was reused for a different immutable intent.</summary>
    public DurableSendId Id { get; }
}
