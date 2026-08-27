namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using Azure.Data.Tables;


    public class AzureTableDatabaseContext<TSaga> :
        DatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        public AzureTableDatabaseContext(TableClient table, ISagaKeyFormatter<TSaga> keyFormatter)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(keyFormatter);

            Table = table;
            Formatter = keyFormatter;

            Converter = EntityConverterFactory.CreateConverter<TSaga>();
        }

        public ISagaKeyFormatter<TSaga> Formatter { get; }
        public TableClient Table { get; }
        public IEntityConverter<TSaga> Converter { get; }

        public (string partitionKey, string rowKey) Format(Guid correlationId)
        {
            AzureTableKeyValidator.ValidateCorrelationId(correlationId, nameof(correlationId));
            (string partitionKey, string rowKey) = Formatter.Format(correlationId);

            return (
                AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey)),
                AzureTableKeyValidator.Validate(rowKey, nameof(rowKey)));
        }
    }
}
