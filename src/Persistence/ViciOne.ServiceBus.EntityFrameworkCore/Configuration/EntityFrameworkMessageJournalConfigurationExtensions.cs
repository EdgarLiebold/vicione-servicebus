using System;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures the bounded message journal to persist entries through EF Core.</summary>
public static class EntityFrameworkMessageJournalConfigurationExtensions
{
    /// <summary>Selects the Entity Framework journal store inside <c>bus.UseMessageJournal(...)</c> without opening a database connection.</summary>
    /// <typeparam name="TDbContext">The db context type.</typeparam>
    /// <param name="configurator">The journal provider selector on which the EF Core store is selected.</param>
    /// <param name="contextOptions">The preconfigured EF Core options used to create journal contexts.</param>
    /// <param name="tableName">The relational table that stores journal entries.</param>
    /// <param name="storeLimits">The entry-size, count, and retention bounds enforced by the store.</param>
    /// <param name="schemaName">The relational schema, or <see langword="null"/> to use the provider default.</param>
    /// <returns>The same journal configurator.</returns>
    public static IMessageJournalConfigurator UseEntityFramework<TDbContext>(
        this IMessageJournalConfigurator configurator,
        DbContextOptions<TDbContext> contextOptions,
        string tableName,
        MessageJournalStoreLimits storeLimits,
        string? schemaName = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextOptions);
        ArgumentNullException.ThrowIfNull(storeLimits);
        return configurator.UseStore(new EntityFrameworkMessageJournalStore(
            contextOptions,
            tableName,
            storeLimits,
            schemaName));
    }

    /// <summary>
    /// Explicitly enables the bounded relational MessageJournal provider. Database schema creation
    /// remains an application/deployment responsibility and never occurs as a configuration side effect.
    /// </summary>
    /// <param name="configurator">The bus journal configuration on which the EF Core store is enabled.</param>
    /// <param name="contextOptions">The preconfigured EF Core options used to create journal contexts.</param>
    /// <param name="tableName">The relational table that stores journal entries.</param>
    /// <param name="policy">The policy that decides which message observations are recorded.</param>
    /// <param name="storeLimits">The entry-size, count, and retention bounds enforced by the store.</param>
    /// <param name="journalOptions">The sanitization and content-capture settings for each entry.</param>
    /// <param name="schemaName">The relational schema, or <see langword="null"/> to use the provider default.</param>
    /// <returns>A handle that disconnects the journal observer.</returns>
    public static ConnectHandle UseEntityFrameworkCoreMessageJournal(
        this IBusFactoryConfigurator configurator,
        DbContextOptions contextOptions,
        string tableName,
        IMessageJournalPolicy policy,
        MessageJournalStoreLimits storeLimits,
        MessageJournalOptions journalOptions,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(contextOptions);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(storeLimits);
        ArgumentNullException.ThrowIfNull(journalOptions);
        var store = new EntityFrameworkMessageJournalStore(
            contextOptions,
            tableName,
            storeLimits,
            schemaName);

        return configurator.ConnectMessageJournal(store, policy, journalOptions);
    }
}
