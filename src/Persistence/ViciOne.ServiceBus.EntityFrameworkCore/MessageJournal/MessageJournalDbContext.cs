using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;

/// <summary>Dedicated bounded message-journal context. It is not a ViciOne Suite audit context.</summary>
public sealed class MessageJournalDbContext : DbContext
{
    private readonly string? _schemaName;
    private readonly string _tableName;

    /// <summary>Initializes a journal context for an explicitly selected table.</summary>
    /// <param name="options">The configured relational provider options.</param>
    /// <param name="tableName">The table that stores journal entries.</param>
    /// <param name="schemaName">The schema, or <see langword="null"/> to use the provider default.</param>
    public MessageJournalDbContext(
        DbContextOptions options,
        string tableName,
        string? schemaName = null)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _tableName = RelationalIdentifierValidator.Validate(tableName, nameof(tableName));
        _schemaName = schemaName is null
            ? null
            : RelationalIdentifierValidator.Validate(schemaName, nameof(schemaName));
    }

    /// <summary>Gets the persisted sanitized journal entries.</summary>
    public DbSet<MessageJournalRecord> Entries => Set<MessageJournalRecord>();

    internal string? SchemaName => _schemaName;

    internal string TableName => _tableName;

    /// <summary>Installs a model-cache key that includes the selected table and schema.</summary>
    /// <param name="optionsBuilder">The context options being configured.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, MessageJournalModelCacheKeyFactory>();
    }

    /// <summary>Maps the journal record to the selected table and schema.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageJournalMapping(_tableName, _schemaName));
    }
}
