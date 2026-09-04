using System;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides a const row saga key formatter implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ConstRowSagaKeyFormatter<TSaga> :
    ISagaKeyFormatter<TSaga>
    where TSaga : class, ISaga
{
    readonly string _rowKey;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="rowKey">The row key value.</param>
    public ConstRowSagaKeyFormatter(string rowKey)
    {
        _rowKey = AzureTableKeyValidator.Validate(rowKey, nameof(rowKey));
    }

    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (correlationId.ToString("D"), _rowKey);
    }
}
