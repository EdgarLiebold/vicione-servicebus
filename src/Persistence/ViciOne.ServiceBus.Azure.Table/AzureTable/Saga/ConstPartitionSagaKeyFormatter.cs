using System;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Stores every saga of one type in a fixed Azure Table partition and uses its correlation identifier as the row key.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ConstPartitionSagaKeyFormatter<TSaga> :
    ISagaKeyFormatter<TSaga>
    where TSaga : class, ISaga
{
    readonly string _partitionKey;

    /// <summary>Creates a formatter with a validated constant partition key.</summary>
    /// <param name="partitionKey">The Azure Table partition shared by the sagas.</param>
    public ConstPartitionSagaKeyFormatter(string partitionKey)
    {
        _partitionKey = AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));
    }

    /// <summary>Uses the configured partition and the canonical correlation identifier as the row key.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The constant partition key and correlation-identifier row key.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (_partitionKey, correlationId.ToString("D"));
    }
}
