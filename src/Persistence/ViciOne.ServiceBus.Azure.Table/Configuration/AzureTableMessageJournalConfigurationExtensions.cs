using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;

#nullable enable
namespace ViciOne.ServiceBus;

public static class AzureTableMessageJournalConfigurationExtensions
{
    /// <summary>
    /// Explicitly enables MessageJournal using an existing TableClient. Table provisioning remains
    /// a deployment responsibility and is never a hidden synchronous configuration side effect.
    /// </summary>
    public static ConnectHandle UseAzureTableMessageJournal(
        this IBusFactoryConfigurator configurator,
        TableClient table,
        AzureTableMessageJournalStoreOptions storeOptions,
        IMessageJournalPolicy policy,
        MessageJournalOptions journalOptions)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        var store = new AzureTableMessageJournalStore(table, storeOptions);
        return configurator.ConnectMessageJournal(store, policy, journalOptions);
    }

    /// <summary>
    /// Explicitly enables MessageJournal from a service client and table name without performing
    /// network I/O during composition.
    /// </summary>
    public static ConnectHandle UseAzureTableMessageJournal(
        this IBusFactoryConfigurator configurator,
        TableServiceClient tableServiceClient,
        string tableName,
        AzureTableMessageJournalStoreOptions storeOptions,
        IMessageJournalPolicy policy,
        MessageJournalOptions journalOptions)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        return configurator.UseAzureTableMessageJournal(
            tableServiceClient.GetTableClient(tableName),
            storeOptions,
            policy,
            journalOptions);
    }
}
