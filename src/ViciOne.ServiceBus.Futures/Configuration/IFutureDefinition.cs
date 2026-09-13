using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines registration and endpoint settings for a future state machine.</summary>
/// <typeparam name="TFuture">The future state-machine type.</typeparam>
public interface IFutureDefinition<TFuture> :
    IFutureDefinition
    where TFuture : class, ISagaStateMachine<FutureState>
{
    /// <summary>Gets or sets the endpoint definition used to host the future.</summary>
    new IEndpointDefinition<TFuture>? EndpointDefinition { get; set; }

    /// <summary>Applies the definition to the future saga on its receive endpoint.</summary>
    /// <param name="endpointConfigurator">The configurator for the hosting receive endpoint.</param>
    /// <param name="sagaConfigurator">The configurator for persisted future state.</param>
    /// <param name="context">The registration context that resolves configuration dependencies.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<FutureState> sagaConfigurator,
        IRegistrationContext context);
}


/// <summary>Exposes the untyped metadata required to register a future state machine.</summary>
[ConsumerRegistrationExclusion]
public interface IFutureDefinition :
    IDefinition
{
    /// <summary>Gets the registered future state-machine type.</summary>
    Type FutureType { get; }

    /// <summary>Gets the endpoint definition used to host the future.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Returns the explicit or convention-formatted endpoint name for the future.</summary>
    /// <param name="formatter">The formatter used when no explicit endpoint name exists.</param>
    /// <returns>The endpoint name that hosts the future.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}
