using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future definition.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public interface IFutureDefinition<TFuture> :
    IFutureDefinition
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Sets the endpoint definition, if available</summary>
    new IEndpointDefinition<TFuture> EndpointDefinition { set; }

    /// <summary>Configure the future on the receive endpoint</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer</param>
    /// <param name="sagaConfigurator">The consumer configurator</param>
    /// <param name="context"></param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context);
}


/// <summary>
/// Defines the contract for future definition.
/// </summary>
[ConsumerRegistrationExclusion]
public interface IFutureDefinition :
    IDefinition
{
    /// <summary>
    /// Gets the future type value.
    /// </summary>
    Type FutureType { get; }

    /// <summary>
    /// Gets the endpoint definition value.
    /// </summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>
    /// Return the endpoint name for the future
    /// </summary>
    /// <param name="formatter"></param>
    /// <returns></returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}
