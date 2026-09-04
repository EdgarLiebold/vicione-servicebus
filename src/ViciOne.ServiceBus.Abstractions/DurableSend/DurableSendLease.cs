using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Fenced ownership lease for one persisted durable-send delivery attempt.
/// </summary>
public readonly record struct DurableSendLease
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="token">The token value.</param>
    /// <param name="expiresAt">The expires at value.</param>
    public DurableSendLease(Guid token, DateTimeOffset expiresAt)
    {
        if (token == Guid.Empty)
            throw new ArgumentException("A durable-send lease token cannot be empty.", nameof(token));

        Token = token;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Gets the token value.
    /// </summary>
    public Guid Token { get; }

    /// <summary>
    /// Gets the expires at value.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }
}
