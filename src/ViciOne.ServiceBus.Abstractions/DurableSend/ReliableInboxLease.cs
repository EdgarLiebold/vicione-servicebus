namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Fenced ownership issued by an inbox store.</summary>
public readonly record struct ReliableInboxLease(Guid Token, DateTimeOffset ExpiresAt);
