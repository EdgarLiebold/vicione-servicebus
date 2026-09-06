namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures instance.</summary>
public interface IInstanceConfigurator :
    IConsumeConfigurator
{
}


/// <summary>Configures instance.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface IInstanceConfigurator<TInstance> :
    IConsumerConfigurator<TInstance>,
    IInstanceConfigurator
    where TInstance : class, IConsumer
{
}
