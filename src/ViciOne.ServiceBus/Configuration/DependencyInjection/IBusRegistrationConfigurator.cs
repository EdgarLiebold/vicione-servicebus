namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers the default bus, its endpoint components, and its transport services.</summary>
public interface IBusRegistrationConfigurator :
    IRegistrationConfigurator
{
}


/// <summary>Registers an independently owned typed bus in the same service collection.</summary>
/// <typeparam name="TBus">The application-facing bus contract.</typeparam>
public interface IBusRegistrationConfigurator<in TBus> :
    IBusRegistrationConfigurator
    where TBus : class, IBus
{
}
