// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using Amazon.DynamoDBv2.DataModel;


    public class DynamoDbSagaRepositoryOptions<TSaga>
        where TSaga : class, ISaga
    {
        public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration)
        {
            Expiration = expiration;
            Config = new DynamoDBOperationConfig { OverrideTableName = tableName };
        }

        public DynamoDBOperationConfig Config { get; private set; }

        public TimeSpan? Expiration { get; }

        public string FormatSagaKey(Guid correlationId)
        {
            return correlationId.ToString();
        }
    }
}
