using System;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>Provides the Azure Table client, key formatter, and entity converter used by a saga repository operation.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the strategy that maps saga correlation identifiers to Azure Table partition and row keys.</summary>
    ISagaKeyFormatter<TSaga> Formatter { get; }

    /// <summary>Gets the Azure Table client used to persist the saga.</summary>
    TableClient Table { get; }

    /// <summary>Gets the converter between saga instances and Azure Table property dictionaries.</summary>
    IEntityConverter<TSaga> Converter { get; }

    /// <summary>Maps a saga correlation identifier to its validated Azure Table keys.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The partition key and row key used to address the saga entity.</returns>
    (string partitionKey, string rowKey) Format(Guid correlationId);
}
