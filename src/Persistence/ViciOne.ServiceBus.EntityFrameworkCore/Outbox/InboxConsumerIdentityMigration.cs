using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Explicitly assigns a retained Classic EF inbox consumer identity to its stable replacement.</summary>
/// <param name="LegacyConsumerId">The retained identity from the previous deployment.</param>
/// <param name="StableConsumerId">The operator-confirmed replacement identity.</param>
public sealed record InboxConsumerIdentityMigration(Guid LegacyConsumerId, Guid StableConsumerId);
