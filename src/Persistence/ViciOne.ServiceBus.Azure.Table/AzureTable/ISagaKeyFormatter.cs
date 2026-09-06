using System;

namespace ViciOne.ServiceBus.AzureTable;

/// <summary>Maps saga correlation identifiers to Azure Table partition and row keys.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaKeyFormatter<in TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Formats the keys used to address a saga entity.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The partition key and row key for the saga entity.</returns>
    (string partitionKey, string rowKey) Format(Guid correlationId);
}
