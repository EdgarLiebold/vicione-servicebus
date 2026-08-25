#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.MessageJournal;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Dedicated bounded message-journal context. It is not a ViciOne Suite audit context.
/// </summary>
public sealed class MessageJournalDbContext : DbContext
{
    private readonly string? _schemaName;
    private readonly string _tableName;

    public MessageJournalDbContext(
        DbContextOptions options,
        string tableName,
        string? schemaName = null)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        if (schemaName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        _tableName = tableName;
        _schemaName = schemaName;
    }

    public DbSet<MessageJournalRecord> Entries => Set<MessageJournalRecord>();

    internal string? SchemaName => _schemaName;

    internal string TableName => _tableName;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, MessageJournalModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageJournalMapping(_tableName, _schemaName));
    }
}
