using System;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Captures the inherited expiration inputs that caused a forwarded message to be discarded.</summary>
internal sealed record ExpiredForwarding(
    DateTimeOffset? InheritedExpirationTime,
    TimeSpan? TimeToLive);
