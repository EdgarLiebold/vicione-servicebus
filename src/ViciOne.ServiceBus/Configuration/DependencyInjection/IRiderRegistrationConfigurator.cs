using System;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers a rider capability and its bus-scoped services.</summary>
public interface IRiderRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>Gets the container registrar that owns the rider's component registrations.</summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>Adds a scoped rider service unless that service type is already registered.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <typeparam name="TService">The scoped service to expose.</typeparam>
    /// <param name="factory">The factory that receives the rider instance and current service provider.</param>
    void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
        where TRider : class, IRider
        where TService : class;

    /// <summary>Registers the factory that creates the rider runtime.</summary>
    /// <typeparam name="TRider">The rider runtime contract.</typeparam>
    /// <param name="riderFactory">The factory that creates the rider for its registration context.</param>
    void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
        where TRider : class, IRider;
}


/// <summary>Registers rider services bound to a typed bus.</summary>
/// <typeparam name="TBus">The application-facing bus contract.</typeparam>
public interface IRiderRegistrationConfigurator<in TBus> :
    IRiderRegistrationConfigurator
    where TBus : class, IBus
{
}
