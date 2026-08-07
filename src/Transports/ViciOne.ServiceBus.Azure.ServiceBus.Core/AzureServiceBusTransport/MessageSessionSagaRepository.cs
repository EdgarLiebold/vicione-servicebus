// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    using Saga;


    public static class MessageSessionSagaRepository
    {
        public static ISagaRepository<T> Create<T>()
            where T : class, ISaga
        {
            var consumeContextFactory = new SagaConsumeContextFactory<MessageSessionContext, T>();

            var repositoryFactory = new MessageSessionSagaRepositoryContextFactory<T>(consumeContextFactory);

            return new SagaRepository<T>(repositoryFactory);
        }
    }
}
