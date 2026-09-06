using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures registration of a typed future state machine.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
public interface IFutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Adds endpoint settings to the future registration.</summary>
    /// <param name="configure">The callback that configures the future's receive endpoint.</param>
    /// <returns>The same future registration configurator.</returns>
    new IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Configures the repository that persists future state.</summary>
    /// <param name="configure">The callback that selects and configures the saga repository.</param>
    /// <returns>The same future registration configurator.</returns>
    IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure);
}


/// <summary>Configures registration of a future selected by runtime type.</summary>
public interface IFutureRegistrationConfigurator
{
    /// <summary>Adds endpoint settings to the future registration.</summary>
    /// <param name="configure">The callback that configures the future's receive endpoint.</param>
    /// <returns>The same future registration configurator.</returns>
    IFutureRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes the future from convention-based endpoint configuration.</summary>
    void ExcludeFromConfigureEndpoints();
}
