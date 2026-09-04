using System;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for entity framework message journal configuration.
/// </summary>
public static class EntityFrameworkMessageJournalConfigurationExtensions
{
    /// <summary>
    /// Explicitly enables the bounded relational MessageJournal provider. Database schema creation
    /// remains an application/deployment responsibility and never occurs as a configuration side effect.
    /// </summary>
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
        var store = new EntityFrameworkMessageJournalStore(
            contextOptions,
            tableName,
            storeLimits,
            schemaName);

        return configurator.ConnectMessageJournal(store, policy, journalOptions);
    }
}
