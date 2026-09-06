using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by future definition.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public interface IFutureDefinition<TFuture> :
    IFutureDefinition
    where TFuture : class, SagaStateMachine<FutureState>
{
    /// <summary>Sets the endpoint definition, if available.</summary>
    new IEndpointDefinition<TFuture> EndpointDefinition { set; }

    /// <summary>Configure the future on the receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="sagaConfigurator">The consumer configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context);
}


/// <summary>Defines the operations required by future definition.</summary>
[ConsumerRegistrationExclusion]
public interface IFutureDefinition :
    IDefinition
{
    /// <summary>Gets the future type.</summary>
    Type FutureType { get; }

    /// <summary>Gets the endpoint definition.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Return the endpoint name for the future.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}
