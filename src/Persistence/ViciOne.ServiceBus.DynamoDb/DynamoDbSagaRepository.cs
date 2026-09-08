using System;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Creates versioned saga repositories backed by Amazon DynamoDB.</summary>
public static class DynamoDbSagaRepository
{
    /// <summary>Creates a repository from immutable settings and a factory for operation-scoped AWS persistence contexts.</summary>
    /// <typeparam name="TSaga">The versioned saga state stored by the repository.</typeparam>
    /// <param name="contextFactory">The factory that supplies a new AWS persistence context for each repository operation. The repository owns and disposes every returned context.</param>
    /// <param name="options">The immutable table, read-consistency, conversion, clock, and time-to-live settings.</param>
    /// <returns>An Amazon DynamoDB-backed saga repository.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contextFactory"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public static ISagaRepository<TSaga> Create<TSaga>(
        Func<IDynamoDBContext> contextFactory,
        DynamoDbSagaRepositoryOptions<TSaga> options)
        where TSaga : class, ISagaVersion
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(options);

        var consumeContextFactory = new SagaConsumeContextFactory<IDynamoDbSagaStore<TSaga>, TSaga>();

        var sagaContextFactory = new DynamoDbSagaContextFactory<TSaga>(contextFactory);
        var repositoryContextFactory = new DynamoDbSagaRepositoryContextFactory<TSaga>(
            sagaContextFactory,
            consumeContextFactory,
            options);

        return new SagaRepository<TSaga>(repositoryContextFactory, loadSagaRepositoryContextFactory: repositoryContextFactory);
    }
}
