using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga definition.
/// </summary>
public interface ISagaDefinition :
    IDefinition
{
    /// <summary>
    /// The saga type
    /// </summary>
    Type SagaType { get; }

    /// <summary>
    /// Gets the endpoint definition value.
    /// </summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>
    /// Return the endpoint name for the consumer, using the specified formatter if necessary.
    /// </summary>
    /// <param name="formatter"></param>
    /// <returns></returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>
/// Defines the contract for saga definition.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaDefinition<TSaga> :
    ISagaDefinition
    where TSaga : class, ISaga
{
    /// <summary>
    /// Sets the endpoint definition, if available
    /// </summary>
    new IEndpointDefinition<TSaga> EndpointDefinition { set; }

    /// <summary>
    /// Configure the consumer on the receive endpoint
    /// </summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer</param>
    /// <param name="sagaConfigurator">The consumer configurator</param>
    /// <param name="context"></param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<TSaga> sagaConfigurator, IRegistrationContext context);
}
