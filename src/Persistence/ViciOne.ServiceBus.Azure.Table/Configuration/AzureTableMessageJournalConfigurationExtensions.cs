using System;
using Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Configures optional message journals backed by one bounded Azure Table partition.</summary>
public static class AzureTableMessageJournalConfigurationExtensions
{
    /// <summary>Selects an existing Azure Table journal store inside <c>bus.UseMessageJournal(...)</c>.</summary>
    /// <param name="configurator">The message-journal configurator to update.</param>
    /// <param name="table">The existing Azure Table client used by the journal store.</param>
    /// <param name="storeOptions">The journal partition and finite storage limits.</param>
    /// <returns>The same configurator after assigning the Azure Table store.</returns>
    public static IMessageJournalConfigurator UseAzureTable(
        this IMessageJournalConfigurator configurator,
        TableClient table,
        AzureTableMessageJournalStoreOptions storeOptions)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        return configurator.UseStore(new AzureTableMessageJournalStore(table, storeOptions));
    }

    /// <summary>Selects an Azure Table journal store inside <c>bus.UseMessageJournal(...)</c> without provisioning the table.</summary>
    /// <param name="configurator">The message-journal configurator to update.</param>
    /// <param name="tableServiceClient">The caller-owned Azure Table service client.</param>
    /// <param name="tableName">The existing table that contains the journal partition.</param>
    /// <param name="storeOptions">The journal partition and finite storage limits.</param>
    /// <returns>The same configurator after assigning the Azure Table store.</returns>
    public static IMessageJournalConfigurator UseAzureTable(
        this IMessageJournalConfigurator configurator,
        TableServiceClient tableServiceClient,
        string tableName,
        AzureTableMessageJournalStoreOptions storeOptions)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(tableServiceClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        return configurator.UseAzureTable(tableServiceClient.GetTableClient(tableName), storeOptions);
    }

    /// <summary>
    /// Explicitly enables MessageJournal using an existing TableClient. Table provisioning remains
    /// a deployment responsibility and is never a hidden synchronous configuration side effect.
    /// </summary>
    /// <param name="configurator">The bus factory configurator to update.</param>
    /// <param name="table">The existing Azure Table client used by the journal store.</param>
    /// <param name="storeOptions">The journal partition and finite storage limits.</param>
    /// <param name="policy">The policy that selects and sanitizes journal entries.</param>
    /// <param name="journalOptions">The journal failure and execution settings.</param>
    /// <returns>A handle that disconnects the journal observer.</returns>
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
    /// <param name="configurator">The bus factory configurator to update.</param>
    /// <param name="tableServiceClient">The caller-owned Azure Table service client.</param>
    /// <param name="tableName">The existing table that contains the journal partition.</param>
    /// <param name="storeOptions">The journal partition and finite storage limits.</param>
    /// <param name="policy">The policy that selects and sanitizes journal entries.</param>
    /// <param name="journalOptions">The journal failure and execution settings.</param>
    /// <returns>A handle that disconnects the journal observer.</returns>
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
