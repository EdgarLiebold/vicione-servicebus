using System;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>
/// Provides a dynamo db saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public static class DynamoDbSagaRepository<TSaga>
    where TSaga : class, ISagaVersion
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="dynamoDbFactory">The dynamo db factory value.</param>
    /// <param name="tableName">The table name value.</param>
    /// <param name="expiration">The expiration value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> Create(Func<IDynamoDBContext> dynamoDbFactory, string tableName, TimeSpan? expiration = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dynamoDbFactory);

        var options = new DynamoDbSagaRepositoryOptions<TSaga>(tableName, expiration, timeProvider ?? TimeProvider.System);

        var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

        var contextFactory = new DynamoDbContextFactory<TSaga>(dynamoDbFactory);
        var repositoryContextFactory = new DynamoDbSagaRepositoryContextFactory<TSaga>(contextFactory, consumeContextFactory, options);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }
}
