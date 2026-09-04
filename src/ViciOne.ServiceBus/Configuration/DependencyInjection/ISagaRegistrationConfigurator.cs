using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga registration configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaRegistrationConfigurator<TSaga> :
    ISagaRegistrationConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    new ISagaRegistrationConfigurator<TSaga> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the repository operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    ISagaRegistrationConfigurator<TSaga> Repository(Action<ISagaRepositoryRegistrationConfigurator<TSaga>> configure);
}


/// <summary>
/// Defines the contract for saga registration configurator.
/// </summary>
public interface ISagaRegistrationConfigurator
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    ISagaRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    void ExcludeFromConfigureEndpoints();
}
