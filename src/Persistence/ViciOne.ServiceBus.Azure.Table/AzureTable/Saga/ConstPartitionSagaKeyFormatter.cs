using System;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>
/// Provides a const partition saga key formatter implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ConstPartitionSagaKeyFormatter<TSaga> :
    ISagaKeyFormatter<TSaga>
    where TSaga : class, ISaga
{
    readonly string _partitionKey;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitionKey">The partition key value.</param>
    public ConstPartitionSagaKeyFormatter(string partitionKey)
    {
        _partitionKey = AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));
    }

    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (_partitionKey, correlationId.ToString("D"));
    }
}
