using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;

/// <summary>Maps the provider-neutral message-journal record to its explicitly selected table.</summary>
public sealed class MessageJournalMapping : IEntityTypeConfiguration<MessageJournalRecord>
{
    private readonly string? _schemaName;
    private readonly string _tableName;

    /// <summary>Initializes a mapping for an explicitly selected journal table.</summary>
    /// <param name="tableName">The table that stores journal entries.</param>
    /// <param name="schemaName">The schema, or <see langword="null"/> to use the provider default.</param>
    public MessageJournalMapping(string tableName, string? schemaName = null)
    {
        _tableName = RelationalIdentifierValidator.Validate(tableName, nameof(tableName));
        _schemaName = schemaName is null
            ? null
            : RelationalIdentifierValidator.Validate(schemaName, nameof(schemaName));
    }

    /// <summary>Configures keys, UTC-tick time persistence, indexes, required fields, and the target table.</summary>
    /// <param name="builder">The journal-record entity builder.</param>
    public void Configure(EntityTypeBuilder<MessageJournalRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (_schemaName is null)
            builder.ToTable(_tableName);
        else
            builder.ToTable(_tableName, _schemaName);

        builder.HasKey(record => record.EntryId);
        builder.Property(record => record.EntryId).ValueGeneratedNever();
        builder.Property(record => record.ObservedAt)
            .HasConversion(
                observedAt => observedAt.UtcTicks,
                utcTicks => new DateTimeOffset(utcTicks, TimeSpan.Zero));
        builder.HasIndex(record => new { record.ObservedAt, record.EntryId });

        builder.Property(record => record.Operation).IsRequired();
        builder.Property(record => record.Outcome).IsRequired();
        builder.Property(record => record.DataClassification).IsRequired();
        builder.Property(record => record.ContentType).HasMaxLength(256);
        builder.Property(record => record.MessageTypesJson).IsRequired();
        builder.Property(record => record.MetadataJson).IsRequired();
        builder.Property(record => record.HeadersJson).IsRequired();
        builder.Property(record => record.Body).IsRequired();
        builder.Property(record => record.ContentSizeInBytes).IsRequired();
    }
}
