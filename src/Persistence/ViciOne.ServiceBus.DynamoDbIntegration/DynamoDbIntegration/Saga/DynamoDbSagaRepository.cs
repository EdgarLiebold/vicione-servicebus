using System;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

public static class DynamoDbSagaRepository<TSaga>
    where TSaga : class, ISagaVersion
{
    public static ISagaRepository<TSaga> Create(Func<IDynamoDBContext> dynamoDbFactory, string tableName, TimeSpan? expiration = null,
        TimeProvider timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbFactory);

        var options = new DynamoDbSagaRepositoryOptions<TSaga>(tableName, expiration, timeProvider ?? TimeProvider.System);

        var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

        var contextFactory = new DynamoDbContextFactory<TSaga>(dynamoDbFactory);
        var repositoryContextFactory = new DynamoDbSagaRepositoryContextFactory<TSaga>(contextFactory, consumeContextFactory, options);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }
}
