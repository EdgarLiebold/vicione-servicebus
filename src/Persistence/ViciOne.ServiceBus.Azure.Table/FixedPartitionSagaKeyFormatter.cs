using System;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Stores every saga of one type in a fixed Azure Table partition and uses its correlation identifier as the row key.</summary>
public sealed class FixedPartitionSagaKeyFormatter :
    IAzureTableSagaKeyFormatter
{
    readonly string _partitionKey;

    /// <summary>Creates a formatter with a validated constant partition key.</summary>
    /// <param name="partitionKey">The Azure Table partition shared by the sagas.</param>
    /// <exception cref="ArgumentNullException"><paramref name="partitionKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="partitionKey"/> is empty or invalid for Azure Table.</exception>
    public FixedPartitionSagaKeyFormatter(string partitionKey)
    {
        _partitionKey = AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));
    }

    /// <summary>Uses the configured partition and the canonical correlation identifier as the row key.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The constant partition key and correlation-identifier row key.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is empty.</exception>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (_partitionKey, correlationId.ToString("D"));
    }
}
