using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future registration configurator.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public interface IFutureRegistrationConfigurator<TFuture> :
    IFutureRegistrationConfigurator
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    new IFutureRegistrationConfigurator<TFuture> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the repository operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IFutureRegistrationConfigurator<TFuture> Repository(Action<ISagaRepositoryRegistrationConfigurator<FutureState>> configure);
}


/// <summary>
/// Defines the contract for future registration configurator.
/// </summary>
public interface IFutureRegistrationConfigurator
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IFutureRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    void ExcludeFromConfigureEndpoints();
}
