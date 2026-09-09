using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Fenced ownership lease for one persisted durable-send delivery attempt.</summary>
public readonly record struct DurableSendLease
{
    /// <summary>Initializes an exclusive delivery lease.</summary>
    /// <param name="token">The nonempty fencing token issued for the claim.</param>
    /// <param name="expiresAt">The instant at which another delivery worker may take ownership.</param>
    /// <exception cref="ArgumentException"><paramref name="token" /> is empty.</exception>
    public DurableSendLease(Guid token, DateTimeOffset expiresAt)
    {
        if (token == Guid.Empty)
            throw new ArgumentException("A durable-send lease token cannot be empty.", nameof(token));

        Token = token;
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the token that fences state changes made by this lease owner.</summary>
    public Guid Token { get; }

    /// <summary>Gets the instant at which the lease stops excluding another claimant.</summary>
    public DateTimeOffset ExpiresAt { get; }
}
