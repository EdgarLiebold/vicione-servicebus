// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
            _rowKey = rowKey;
        }

        public (string partitionKey, string rowKey) Format(Guid correlationId)
        {
            return (correlationId.ToString(), _rowKey);
        }
    }
}
