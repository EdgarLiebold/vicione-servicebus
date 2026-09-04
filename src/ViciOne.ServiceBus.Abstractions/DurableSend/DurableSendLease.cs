using System;
using System.ComponentModel;

namespace ViciOne.ServiceBus;

/// <summary>
/// Fenced ownership lease for one persisted durable-send delivery attempt.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct DurableSendLease
{
    public DurableSendLease(Guid token, DateTimeOffset expiresAt)
    {
        if (token == Guid.Empty)
            throw new ArgumentException("A durable-send lease token cannot be empty.", nameof(token));

        Token = token;
        ExpiresAt = expiresAt;
    }

    public Guid Token { get; }

    public DateTimeOffset ExpiresAt { get; }
}
