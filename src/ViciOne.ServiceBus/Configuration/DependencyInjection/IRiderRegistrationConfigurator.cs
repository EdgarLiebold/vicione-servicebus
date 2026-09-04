using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public interface IRiderRegistrationConfigurator :
    IRegistrationConfigurator
{
    IContainerRegistrar Registrar { get; }

    void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
        where TRider : class, IRider
        where TService : class;

    /// <summary>
    /// Add the rider to the container, configured properly
    /// </summary>
    /// <param name="riderFactory"></param>
    void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
        where TRider : class, IRider;
}


public interface IRiderRegistrationConfigurator<in TBus> :
    IRiderRegistrationConfigurator
    where TBus : class, IBus
{
}
