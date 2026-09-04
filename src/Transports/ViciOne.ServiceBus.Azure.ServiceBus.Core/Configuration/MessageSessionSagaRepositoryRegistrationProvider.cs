namespace ViciOne.ServiceBus.Configuration;

public class MessageSessionSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.MessageSessionRepository();
    }
}
