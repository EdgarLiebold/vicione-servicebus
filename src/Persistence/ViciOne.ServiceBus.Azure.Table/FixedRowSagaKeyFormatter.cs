using System;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Uses each saga correlation identifier as an Azure Table partition key and stores the saga under a fixed row key.</summary>
public sealed class FixedRowSagaKeyFormatter :
    IAzureTableSagaKeyFormatter
{
    readonly string _rowKey;

    /// <summary>Creates a formatter with a validated constant row key.</summary>
    /// <param name="rowKey">The row key shared by all saga partitions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rowKey"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="rowKey"/> is empty or invalid for Azure Table.</exception>
    public FixedRowSagaKeyFormatter(string rowKey)
    {
        _rowKey = AzureTableKeyValidator.Validate(rowKey, nameof(rowKey));
    }

    /// <summary>Uses the canonical correlation identifier as the partition and the configured row key.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <returns>The correlation-identifier partition key and constant row key.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is empty.</exception>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (correlationId.ToString("D"), _rowKey);
    }
}
