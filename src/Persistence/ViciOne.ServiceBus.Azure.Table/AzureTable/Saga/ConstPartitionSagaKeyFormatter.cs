using System;

namespace ViciOne.ServiceBus.AzureTable.Saga;

public class ConstPartitionSagaKeyFormatter<TSaga> :
    ISagaKeyFormatter<TSaga>
    where TSaga : class, ISaga
{
    readonly string _partitionKey;

    public ConstPartitionSagaKeyFormatter(string partitionKey)
    {
        _partitionKey = AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));
    }

    public (string partitionKey, string rowKey) Format(Guid correlationId)
    {
        AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
        return (_partitionKey, correlationId.ToString("D"));
    }
}
