namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies Azure Service Bus session persistence to convention-based saga registrations.</summary>
public class MessageSessionSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    /// <summary>Configures the saga registration to store state in its Azure Service Bus session.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="configurator">The saga registration to configure.</param>
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.MessageSessionRepository();
    }
}
