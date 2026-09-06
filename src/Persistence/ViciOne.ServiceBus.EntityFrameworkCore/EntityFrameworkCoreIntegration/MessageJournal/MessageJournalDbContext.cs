using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
/// <summary>
/// Dedicated bounded message-journal context. It is not a ViciOne Suite audit context.
/// </summary>
public sealed class MessageJournalDbContext : DbContext
{
    private readonly string? _schemaName;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="tableName">The table name value.</param>
    /// <param name="schemaName">The schema name value.</param>
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

    /// <summary>
    /// Gets the entries value.
    /// </summary>
    public DbSet<MessageJournalRecord> Entries => Set<MessageJournalRecord>();

    internal string? SchemaName => _schemaName;

    internal string TableName => _tableName;

    /// <summary>
    /// Performs the on configuring operation.
    /// </summary>
    /// <param name="optionsBuilder">The options builder value.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, MessageJournalModelCacheKeyFactory>();
    }

    /// <summary>
    /// Performs the on model creating operation.
    /// </summary>
    /// <param name="modelBuilder">The model builder value.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageJournalMapping(_tableName, _schemaName));
    }
}
