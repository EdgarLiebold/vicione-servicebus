// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MartenIntegration.Saga
{
    using Marten;
    using ViciOne.ServiceBus.Saga;


    public static class MartenSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        public static ISagaRepository<TSaga> Create(IDocumentStore documentStore)
        {
            var consumeContextFactory = new SagaConsumeContextFactory<IDocumentSession, TSaga>();

            var repositoryContextFactory = new MartenSagaRepositoryContextFactory<TSaga>(documentStore, consumeContextFactory);
            return new SagaRepository<TSaga>(repositoryContextFactory, repositoryContextFactory, repositoryContextFactory);
        }
    }
}
