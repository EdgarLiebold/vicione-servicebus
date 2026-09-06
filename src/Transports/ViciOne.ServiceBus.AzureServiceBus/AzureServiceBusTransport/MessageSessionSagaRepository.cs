using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates saga repositories that store one saga state in the active Azure Service Bus session.</summary>
public static class MessageSessionSagaRepository
{
    /// <summary>Creates a session-backed repository for a saga state type.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <returns>The session-backed saga repository.</returns>
    public static ISagaRepository<T> Create<T>()
        where T : class, ISaga
    {
        var consumeContextFactory = new SagaConsumeContextFactory<MessageSessionContext, T>();

        var repositoryFactory = new MessageSessionSagaRepositoryContextFactory<T>(consumeContextFactory);

        return new SagaRepository<T>(repositoryFactory);
    }
}
