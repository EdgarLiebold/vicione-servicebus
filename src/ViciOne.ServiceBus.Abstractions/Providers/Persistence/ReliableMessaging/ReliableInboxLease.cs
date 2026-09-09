namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Fenced ownership issued by an inbox store.</summary>
public readonly record struct ReliableInboxLease
{
    /// <summary>Creates an exclusive inbox-processing lease.</summary>
    /// <param name="token">The nonempty fencing token.</param>
    /// <param name="expiresAt">The instant at which another processing attempt may acquire the record.</param>
    /// <exception cref="ArgumentException"><paramref name="token" /> is empty.</exception>
    public ReliableInboxLease(Guid token, DateTimeOffset expiresAt)
    {
        if (token == Guid.Empty)
            throw new ArgumentException("An inbox lease token cannot be empty.", nameof(token));

        Token = token;
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the fencing token.</summary>
    public Guid Token { get; }

    /// <summary>Gets the instant at which another processing attempt may acquire the record.</summary>
    public DateTimeOffset ExpiresAt { get; }
}
