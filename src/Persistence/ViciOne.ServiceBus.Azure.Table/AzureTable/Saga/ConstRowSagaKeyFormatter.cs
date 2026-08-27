namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;


    public class ConstRowSagaKeyFormatter<TSaga> :
        ISagaKeyFormatter<TSaga>
        where TSaga : class, ISaga
    {
        readonly string _rowKey;

        public ConstRowSagaKeyFormatter(string rowKey)
        {
            _rowKey = AzureTableKeyValidator.Validate(rowKey, nameof(rowKey));
        }

        public (string partitionKey, string rowKey) Format(Guid correlationId)
        {
            AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
            return (correlationId.ToString("D"), _rowKey);
        }
    }
}
