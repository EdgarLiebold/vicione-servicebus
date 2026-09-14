using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides rider-scoped registration lookup while sharing endpoint attachment state with the bus.</summary>
internal sealed class RiderRegistrationContext :
    ISetScopedConsumeContext,
    IRiderRegistrationContext
{
    readonly RegistrationContext _registration;

    readonly IContainerSelector _selector;

    /// <summary>Creates a rider view over an existing bus registration context.</summary>
    /// <param name="registration">The bus registration context that owns attachment state.</param>
    /// <param name="selector">The rider-specific component registration selector.</param>
    public RiderRegistrationContext(RegistrationContext registration, IContainerSelector selector)
    {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <summary>Returns rider registrations of the requested contract.</summary>
    /// <typeparam name="T">The registration contract.</typeparam>
    /// <returns>The registrations visible through the rider selector.</returns>
    public IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration
    {
        return _selector.GetRegistrations<T>(_registration);
    }

    /// <summary>Resolves a service from the shared bus registration context.</summary>
    /// <param name="serviceType">The requested service contract.</param>
    /// <returns>The resolved service, or <see langword="null" /> when the contract is not registered.</returns>
    public object? GetService(Type serviceType)
    {
        return _registration.GetService(serviceType);
    }

    /// <summary>Attaches a runtime-selected registered consumer to a rider endpoint.</summary>
    /// <param name="consumerType">The registered consumer implementation.</param>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureConsumer(Type consumerType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumer(consumerType, configurator);
    }

    /// <summary>Attaches a registered consumer to a rider endpoint.</summary>
    /// <typeparam name="T">The registered consumer implementation.</typeparam>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    /// <param name="configure">An optional callback applied to the consumer before attachment.</param>
    public void ConfigureConsumer<T>(IReceiveEndpointConfigurator configurator, Action<IConsumerConfigurator<T>>? configure = null)
        where T : class, IConsumer
    {
        _registration.ConfigureConsumer(configurator, configure);
    }

    /// <summary>Attaches every registered consumer not already configured by the shared context.</summary>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureConsumers(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumers(configurator);
    }

    /// <summary>Attaches every registered consumer-kind capability to a rider endpoint.</summary>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureConsumerKinds(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureConsumerKinds(configurator);
    }

    /// <summary>Attaches a runtime-selected registered saga to a rider endpoint.</summary>
    /// <param name="sagaType">The registered saga implementation.</param>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureSaga(Type sagaType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSaga(sagaType, configurator);
    }

    /// <summary>Attaches a registered saga to a rider endpoint.</summary>
    /// <typeparam name="T">The registered saga implementation.</typeparam>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    /// <param name="configure">An optional callback applied to the saga before attachment.</param>
    public void ConfigureSaga<T>(IReceiveEndpointConfigurator configurator, Action<ISagaConfigurator<T>>? configure = null)
        where T : class
    {
        _registration.ConfigureSaga(configurator, configure);
    }

    /// <summary>Attaches every registered saga not already configured by the shared context.</summary>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureSagas(IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureSagas(configurator);
    }

    /// <summary>Attaches a runtime-selected routing-slip execute activity to a rider endpoint.</summary>
    /// <param name="activityType">The registered execute-activity implementation.</param>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureExecuteActivity(Type activityType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureExecuteActivity(activityType, configurator);
    }

    /// <summary>Attaches both endpoints of a compensating routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="executeEndpointConfigurator">The execute receive endpoint.</param>
    /// <param name="compensateEndpointConfigurator">The compensate receive endpoint.</param>
    public void ConfigureActivity(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator,
        IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivity(activityType, executeEndpointConfigurator, compensateEndpointConfigurator);
    }

    /// <summary>Attaches the execute endpoint of a compensating activity and links its compensate address.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="executeEndpointConfigurator">The execute receive endpoint.</param>
    /// <param name="compensateAddress">The destination of the companion compensate endpoint.</param>
    public void ConfigureActivityExecute(Type activityType, IReceiveEndpointConfigurator executeEndpointConfigurator, Uri compensateAddress)
    {
        _registration.ConfigureActivityExecute(activityType, executeEndpointConfigurator, compensateAddress);
    }

    /// <summary>Attaches the compensate endpoint of a registered routing-slip activity.</summary>
    /// <param name="activityType">The registered activity implementation.</param>
    /// <param name="compensateEndpointConfigurator">The compensate receive endpoint.</param>
    public void ConfigureActivityCompensate(Type activityType, IReceiveEndpointConfigurator compensateEndpointConfigurator)
    {
        _registration.ConfigureActivityCompensate(activityType, compensateEndpointConfigurator);
    }

    /// <summary>Attaches a runtime-selected registered future to a rider endpoint.</summary>
    /// <param name="futureType">The registered future implementation.</param>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureFuture(Type futureType, IReceiveEndpointConfigurator configurator)
    {
        _registration.ConfigureFuture(futureType, configurator);
    }

    /// <summary>Attaches a registered future to a rider endpoint.</summary>
    /// <typeparam name="T">The registered future implementation.</typeparam>
    /// <param name="configurator">The rider receive endpoint being configured.</param>
    public void ConfigureFuture<T>(IReceiveEndpointConfigurator configurator)
        where T : class
    {
        _registration.ConfigureFuture<T>(configurator);
    }

    /// <summary>Installs a consume context for the lifetime of a rider message scope.</summary>
    /// <param name="scope">The active rider message scope.</param>
    /// <param name="context">The consume context exposed within that scope.</param>
    /// <returns>A handle that restores the previous ambient context when disposed.</returns>
    public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
    {
        return _registration.PushContext(scope, context);
    }
}
