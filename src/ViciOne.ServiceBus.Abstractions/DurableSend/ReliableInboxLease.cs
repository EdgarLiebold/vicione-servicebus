namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Fenced ownership issued by an inbox store.</summary>
/// <param name="Token">The token.</param>
/// <param name="ExpiresAt">The expires at.</param>
public readonly record struct ReliableInboxLease(Guid Token, DateTimeOffset ExpiresAt);
