namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory saga repository registration provider implementation.
/// </summary>
public class InMemorySagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.InMemoryRepository();
    }
}
