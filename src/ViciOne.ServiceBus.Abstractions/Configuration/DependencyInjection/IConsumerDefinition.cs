using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Describes how a registered consumer is named and configured on a receive endpoint.</summary>
public interface IConsumerDefinition :
    IDefinition
{
    /// <summary>Gets the registered consumer implementation type.</summary>
    Type ConsumerType { get; }

    /// <summary>Gets the consumer's dedicated endpoint definition, when one was configured.</summary>
    IEndpointDefinition? EndpointDefinition { get; }

    /// <summary>Gets the configured endpoint name or derives one with the supplied formatter.</summary>
    /// <param name="formatter">The naming convention used when no explicit name is configured.</param>
    /// <returns>The consumer endpoint name.</returns>
    string GetEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>Describes endpoint configuration for a specific registered consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerDefinition<TConsumer> :
    IConsumerDefinition
    where TConsumer : class, IConsumer
{
    /// <summary>Sets the consumer's dedicated endpoint definition.</summary>
    new IEndpointDefinition<TConsumer> EndpointDefinition { set; }

    /// <summary>Applies consumer-specific configuration to a receive endpoint.</summary>
    /// <param name="endpointConfigurator">The receive endpoint configurator for the consumer.</param>
    /// <param name="consumerConfigurator">The consumer pipeline to configure.</param>
    /// <param name="context">The registration context that resolves registered dependencies.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context);
}
