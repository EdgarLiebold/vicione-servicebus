namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga repository registration provider.
/// </summary>
public interface ISagaRepositoryRegistrationProvider
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga;
}
