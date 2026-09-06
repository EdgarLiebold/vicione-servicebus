using System;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Uses each saga correlation identifier as an Azure Table partition key and stores the saga under a fixed row key.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ConstRowSagaKeyFormatter<TSaga> :
    ISagaKeyFormatter<TSaga>
    where TSaga : class, ISaga
{
    readonly string _rowKey;

    /// <summary>Creates a formatter with a validated constant row key.</summary>
    /// <param name="rowKey">The row key shared by all saga partitions.</param>
    public ConstRowSagaKeyFormatter(string rowKey)
    {
        _rowKey = AzureTableKeyValidator.Validate(rowKey, nameof(rowKey));
    }

    /// <summary>Uses the canonical correlation identifier as the partition and the configured row key.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The correlation-identifier partition key and constant row key.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (correlationId.ToString("D"), _rowKey);
    }
}
