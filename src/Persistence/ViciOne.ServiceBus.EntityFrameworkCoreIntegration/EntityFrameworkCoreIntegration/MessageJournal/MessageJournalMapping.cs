using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.MessageJournal;
/// <summary>
/// Maps the provider-neutral message-journal record to its explicitly selected table.
/// </summary>
public sealed class MessageJournalMapping : IEntityTypeConfiguration<MessageJournalRecord>
{
    private readonly string? _schemaName;
    private readonly string _tableName;

    public MessageJournalMapping(string tableName, string? schemaName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        if (schemaName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        _tableName = tableName;
        _schemaName = schemaName;
    }

    public void Configure(EntityTypeBuilder<MessageJournalRecord> builder)
    {
        if (_schemaName is null)
            builder.ToTable(_tableName);
        else
            builder.ToTable(_tableName, _schemaName);

        builder.HasKey(record => record.EntryId);
        builder.Property(record => record.EntryId).ValueGeneratedNever();
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
