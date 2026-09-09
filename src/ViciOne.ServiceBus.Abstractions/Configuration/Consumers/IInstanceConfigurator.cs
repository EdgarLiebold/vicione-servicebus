namespace ViciOne.ServiceBus.Configuration;

/// <summary>Marks configuration shared by an existing consumer instance.</summary>
public interface IInstanceConfigurator :
    IConsumeConfigurator
{
}


/// <summary>Configures middleware for an existing consumer instance.</summary>
/// <typeparam name="TInstance">The consumer-instance type.</typeparam>
public interface IInstanceConfigurator<TInstance> :
    IConsumerConfigurator<TInstance>,
    IInstanceConfigurator
    where TInstance : class, IConsumer
{
}
