namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the container registration, and supports creation of a bus or a mediator.
/// </summary>
public interface IBusRegistrationConfigurator :
    IRegistrationConfigurator
{
}


/// <summary>
/// Configures additional bus instances, configured via MultiBus
/// </summary>
/// <typeparam name="TBus">The additional bus interface type</typeparam>
public interface IBusRegistrationConfigurator<in TBus> :
    IBusRegistrationConfigurator
    where TBus : class, IBus
{
}
