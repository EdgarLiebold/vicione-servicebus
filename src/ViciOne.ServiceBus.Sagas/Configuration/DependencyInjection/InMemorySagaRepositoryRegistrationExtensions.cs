using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for in memory saga repository registration.</summary>
public static class InMemorySagaRepositoryRegistrationExtensions
{
    /// <summary>Adds an in-memory saga repository to the registration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<T> InMemoryRepository<T>(this ISagaRegistrationConfigurator<T> configurator)
        where T : class, ISaga
    {
        configurator.Repository(x => x.RegisterInMemorySagaRepository<T>());

        return configurator;
    }

    /// <summary>Use the InMemorySagaRepository for sagas configured by type (without a specific generic call to AddSaga/AddSagaStateMachine).</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void SetInMemorySagaRepositoryProvider(this IRegistrationConfigurator configurator)
    {
        configurator.SetSagaRepositoryProvider(new InMemorySagaRepositoryRegistrationProvider());
    }
}
