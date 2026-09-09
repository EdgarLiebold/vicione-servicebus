using System;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>EF model mapping for the unified ViciOne reliable-messaging store.</summary>
public static class EntityFrameworkReliableMessagingModelExtensions
{
    /// <summary>Adds outbox, inbox, capacity-ledger, and recurring-schedule mappings to an EF Core model.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="schema">The schema for all reliable-messaging tables, or <see langword="null"/> for the provider default.</param>
    /// <returns>The same model builder.</returns>
    public static ModelBuilder AddViciOneReliableMessaging(this ModelBuilder modelBuilder, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        if (schema is not null)
            RelationalIdentifierValidator.Validate(schema, nameof(schema));

        var record = modelBuilder.Entity<DurableSendRecord>();
        record.ToTable("vicione_outbox", schema);
        record.HasKey(x => new { x.StoreKey, x.Id });
        record.Property(x => x.StoreKey).HasMaxLength(128);
        record.Property(x => x.ContractIdentity).HasMaxLength(320);
        record.Property(x => x.DestinationAddress).HasMaxLength(SerializedDurableSend.MaximumDestinationAddressCharacters);
        record.Property(x => x.ContentType).HasMaxLength(SerializedDurableSend.MaximumContentTypeCharacters);
        record.Property(x => x.LastFailureType).HasMaxLength(512);
        record.HasIndex(x => new { x.StoreKey, x.Status, x.NextAttemptAt, x.EnqueuedAt });
        record.HasIndex(x => new { x.StoreKey, x.LeaseExpiresAt });

        var capacity = modelBuilder.Entity<DurableSendCapacityState>();
        capacity.ToTable("vicione_reliable_capacity", schema);
        capacity.HasKey(x => x.StoreKey);
        capacity.Property(x => x.StoreKey).HasMaxLength(128);
        capacity.Property(x => x.StoredCount).IsConcurrencyToken();
        capacity.Property(x => x.StoredBytes).IsConcurrencyToken();

        var inbox = modelBuilder.Entity<ReliableInboxRecord>();
        inbox.ToTable("vicione_inbox", schema);
        inbox.HasKey(x => new { x.StoreKey, x.MessageId, x.ConsumerId });
        inbox.Property(x => x.StoreKey).HasMaxLength(128);
        inbox.Property(x => x.FailureType).HasMaxLength(512);
        inbox.HasIndex(x => new { x.StoreKey, x.Status, x.DueAt, x.ReceivedAt });
        inbox.HasIndex(x => new { x.StoreKey, x.LeaseExpiresAt });

        var schedules = modelBuilder.Entity<ReliableRecurringScheduleRecord>();
        schedules.ToTable("vicione_recurring_schedule", schema);
        schedules.HasKey(x => new { x.StoreKey, x.ScheduleId });
        schedules.Property(x => x.StoreKey).HasMaxLength(128);
        schedules.Property(x => x.CronExpression).HasMaxLength(128);
        schedules.Property(x => x.TimeZoneId).HasMaxLength(128);
        schedules.HasIndex(x => new { x.StoreKey, x.IsPaused, x.NextDueAt });

        return modelBuilder;
    }
}
