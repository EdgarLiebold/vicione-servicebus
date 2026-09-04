using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for rider registration configurator.
/// </summary>
public interface IRiderRegistrationConfigurator :
    IRegistrationConfigurator
{
    /// <summary>
    /// Gets the registrar value.
    /// </summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>
    /// Performs the try add scoped operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <typeparam name="TService">The t service type.</typeparam>
    /// <param name="factory">The factory value.</param>
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


/// <summary>
/// Defines the contract for rider registration configurator.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public interface IRiderRegistrationConfigurator<in TBus> :
    IRiderRegistrationConfigurator
    where TBus : class, IBus
{
}
