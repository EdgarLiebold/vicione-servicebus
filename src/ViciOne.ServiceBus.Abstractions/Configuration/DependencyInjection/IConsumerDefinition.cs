using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Defines the operations required by consumer definition.</summary>
public interface IConsumerDefinition :
    IDefinition
{
    /// <summary>The consumer type.</summary>
    Type ConsumerType { get; }

    /// <summary>Gets the endpoint definition.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Return the endpoint name for the consumer, using the specified formatter if necessary.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>Defines the operations required by consumer definition.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerDefinition<TConsumer> :
    IConsumerDefinition
    where TConsumer : class, IConsumer
{
    /// <summary>Sets the endpoint definition, if available.</summary>
    new IEndpointDefinition<TConsumer> EndpointDefinition { set; }

    /// <summary>Configure the consumer on the receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="consumerConfigurator">The consumer configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context);
}
