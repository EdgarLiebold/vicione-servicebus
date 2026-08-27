namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using ViciOne.ServiceBus.Saga;


    public class AzureTableSagaRepositoryContextFactory<TSaga> :
        ISagaRepositoryContextFactory<TSaga>,
        ILoadSagaRepositoryContextFactory<TSaga>
        where TSaga : class, ISaga
    {
        readonly ICloudTableProvider<TSaga> _cloudTableProvider;
        readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;
        readonly ISagaKeyFormatter<TSaga> _keyFormatter;

        public AzureTableSagaRepositoryContextFactory(ICloudTableProvider<TSaga> cloudTableProvider,
            ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
            ISagaKeyFormatter<TSaga> keyFormatter)
        {
            ArgumentNullException.ThrowIfNull(cloudTableProvider);
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(keyFormatter);

            _cloudTableProvider = cloudTableProvider;
            _factory = factory;
            _keyFormatter = keyFormatter;
        }

        public AzureTableSagaRepositoryContextFactory(TableClient cloudTable,
            ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
            ISagaKeyFormatter<TSaga> keyFormatter)
            : this(new ConstCloudTableProvider<TSaga>(cloudTable), factory, keyFormatter)
        {
        }

        public Task<T> Execute<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(asyncMethod);

            var database = _cloudTableProvider.GetCloudTable();

            var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);
            var repositoryContext = new AzureTableLoadSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

            return asyncMethod(repositoryContext);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.Add("persistence", "azuretable");
        }

        public async Task Send<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            var database = _cloudTableProvider.GetCloudTable();

            var databaseContext = new AzureTableDatabaseContext<TSaga>(database, _keyFormatter);

            var repositoryContext = new AzureTableSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);

            await next.Send(repositoryContext).ConfigureAwait(false);
        }

        public async Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
            where T : class
        {
            throw new NotImplementedByDesignException("Azure Table repository does not support queries");
        }
    }
}
