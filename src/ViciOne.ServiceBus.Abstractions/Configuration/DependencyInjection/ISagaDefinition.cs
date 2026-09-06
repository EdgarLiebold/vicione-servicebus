using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga definition.</summary>
public interface ISagaDefinition :
    IDefinition
{
    /// <summary>The saga type.</summary>
    Type SagaType { get; }

    /// <summary>Gets the endpoint definition.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Return the endpoint name for the consumer, using the specified formatter if necessary.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>Defines the operations required by saga definition.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaDefinition<TSaga> :
    ISagaDefinition
    where TSaga : class, ISaga
{
    /// <summary>Sets the endpoint definition, if available.</summary>
    new IEndpointDefinition<TSaga> EndpointDefinition { set; }

    /// <summary>Configure the consumer on the receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="sagaConfigurator">The consumer configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<TSaga> sagaConfigurator, IRegistrationContext context);
}
