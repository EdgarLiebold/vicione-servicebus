using System;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Creates versioned saga repositories backed by Amazon DynamoDB.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public static class DynamoDbSagaRepository<TSaga>
    where TSaga : class, ISagaVersion
{
    /// <summary>Creates a repository with caller-defined context resolution and optional document expiration.</summary>
    /// <param name="dynamoDbFactory">The factory that supplies the AWS object-persistence context for each repository operation.</param>
    /// <param name="tableName">The Amazon DynamoDB table that stores saga documents.</param>
    /// <param name="expiration">An optional relative lifetime written to each saga document.</param>
    /// <param name="timeProvider">The time source used to calculate document expiration, or <see langword="null"/> to use system time.</param>
    /// <returns>An Amazon DynamoDB-backed saga repository.</returns>
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
