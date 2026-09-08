using System;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Maps saga correlation identifiers to Azure Table partition and row keys.</summary>
public interface IAzureTableSagaKeyFormatter
{
    /// <summary>Formats the keys used to address a saga entity.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The partition key and row key for the saga entity.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is empty.</exception>
    (string partitionKey, string rowKey) Format(Guid correlationId);
}
