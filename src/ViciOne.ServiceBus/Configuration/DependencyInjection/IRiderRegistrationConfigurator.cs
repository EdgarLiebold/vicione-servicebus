using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures rider registration.</summary>
public interface IRiderRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>Gets the registrar.</summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>Attempts to add scoped.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory invoked by the operation.</param>
    void TryAddScoped<TRider, TService>(Func<TRider, IServiceProvider, TService> factory)
        where TRider : class, IRider
        where TService : class;

    /// <summary>Add the rider to the container, configured properly.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderFactory">The rider factory.</param>
    void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
        where TRider : class, IRider;
}


/// <summary>Configures rider registration.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IRiderRegistrationConfigurator<in TBus> :
    IRiderRegistrationConfigurator
    where TBus : class, IBus
{
}
