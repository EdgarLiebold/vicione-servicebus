using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a message session saga repository implementation.
/// </summary>
public static class MessageSessionSagaRepository
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<T> Create<T>()
        where T : class, ISaga
    {
        var consumeContextFactory = new SagaConsumeContextFactory<MessageSessionContext, T>();

        var repositoryFactory = new MessageSessionSagaRepositoryContextFactory<T>(consumeContextFactory);

        return new SagaRepository<T>(repositoryFactory);
    }
}
