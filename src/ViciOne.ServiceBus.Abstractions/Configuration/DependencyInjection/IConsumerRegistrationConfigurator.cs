using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer registration configurator.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public interface IConsumerRegistrationConfigurator<TConsumer> :
    IConsumerRegistrationConfigurator
    where TConsumer : class, IConsumer
{
}


/// <summary>
/// Defines the contract for consumer registration configurator.
/// </summary>
public interface IConsumerRegistrationConfigurator
{
    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    void ExcludeFromConfigureEndpoints();
}
