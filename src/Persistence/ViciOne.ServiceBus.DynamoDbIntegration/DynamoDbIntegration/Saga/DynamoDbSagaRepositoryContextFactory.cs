using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

internal class DynamoDbSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISagaVersion
{
    readonly DynamoDbContextFactory<TSaga> _databaseFactory;
    readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;
    readonly DynamoDbSagaRepositoryOptions<TSaga> _options;

    public DynamoDbSagaRepositoryContextFactory(DynamoDbContextFactory<TSaga> databaseFactory, ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
        DynamoDbSagaRepositoryOptions<TSaga> options)
    {
        _databaseFactory = databaseFactory ?? throw new ArgumentNullException(nameof(databaseFactory));

        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<T> Execute<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        var database = _databaseFactory.Create();

        var databaseContext = new DynamoDbDatabaseContext<TSaga>(database, _options);
        try
        {
            var repositoryContext = new DynamoDbSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

            return await asyncMethod(repositoryContext).ConfigureAwait(false);
        }
        finally
        {
            databaseContext.Dispose();
        }
    }

    public void Probe(ProbeContext context)
    {
        context.Add("persistence", "dynamodb");
    }

    public async Task Send<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        var database = _databaseFactory.Create();

        var databaseContext = new DynamoDbDatabaseContext<TSaga>(database, _options);
        try
        {
            var repositoryContext = new DynamoDbSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);

            await next.Send(repositoryContext).ConfigureAwait(false);
        }
        finally
        {
            databaseContext.Dispose();
        }
    }

    public async Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        throw new NotImplementedByDesignException("DynamoDb saga repository does not support queries");
    }
}
