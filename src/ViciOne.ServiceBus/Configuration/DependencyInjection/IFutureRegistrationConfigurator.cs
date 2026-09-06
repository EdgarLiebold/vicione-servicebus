using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future registration.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public interface IFutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    new IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Applies the repository configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure);
}


/// <summary>Configures future registration.</summary>
public interface IFutureRegistrationConfigurator
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future registration configurator produced by the operation.</returns>
    IFutureRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes from configure endpoints.</summary>
    void ExcludeFromConfigureEndpoints();
}
