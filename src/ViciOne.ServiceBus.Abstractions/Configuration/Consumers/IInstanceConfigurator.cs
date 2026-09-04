namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for instance configurator.
/// </summary>
public interface IInstanceConfigurator :
    IConsumeConfigurator
{
}


/// <summary>
/// Defines the contract for instance configurator.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface IInstanceConfigurator<TInstance> :
    IConsumerConfigurator<TInstance>,
    IInstanceConfigurator
    where TInstance : class, IConsumer
{
}
