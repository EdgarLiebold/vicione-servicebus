#nullable enable

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
using Microsoft.EntityFrameworkCore;

/// <summary>EF model mapping for the generic ViciOne durable sender store.</summary>
public static class EntityFrameworkDurableSendModelExtensions
{
    public static ModelBuilder AddViciOneDurableSender(this ModelBuilder modelBuilder, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var record = modelBuilder.Entity<DurableSendRecord>();
        record.ToTable("DurableSend", schema);
        record.HasKey(x => new { x.StoreKey, x.Id });
        record.Property(x => x.StoreKey).HasMaxLength(128);
        record.Property(x => x.ContractIdentity).HasMaxLength(320);
        record.Property(x => x.DestinationAddress).HasMaxLength(SerializedDurableSend.MaximumDestinationAddressCharacters);
        record.Property(x => x.ContentType).HasMaxLength(SerializedDurableSend.MaximumContentTypeCharacters);
        record.Property(x => x.LastFailureType).HasMaxLength(512);
        record.HasIndex(x => new { x.StoreKey, x.Status, x.NextAttemptAt, x.EnqueuedAt });
        record.HasIndex(x => new { x.StoreKey, x.LeaseExpiresAt });

        var capacity = modelBuilder.Entity<DurableSendCapacityState>();
        capacity.ToTable("DurableSendCapacity", schema);
        capacity.HasKey(x => x.StoreKey);
        capacity.Property(x => x.StoreKey).HasMaxLength(128);

        return modelBuilder;
    }
}
