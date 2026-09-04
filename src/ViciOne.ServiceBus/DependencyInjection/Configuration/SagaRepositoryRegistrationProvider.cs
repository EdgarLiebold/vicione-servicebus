namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga repository registration provider implementation.
/// </summary>
public class SagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
    }
}
