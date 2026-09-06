using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Defines configuration for message handler consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageHandlerConsumerDefinition<TConsumer, TMessage> :
    IConsumerDefinition<TConsumer>
    where TMessage : class
    where TConsumer : class, IConsumer
{
    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => default;
    /// <summary>Gets the consumer type.</summary>
    public Type ConsumerType => typeof(TConsumer);

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="consumerConfigurator">The consumer configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
    }

    /// <summary>Gets or sets the endpoint definition.</summary>
    public IEndpointDefinition<TConsumer>? EndpointDefinition { get; set; }

    IEndpointDefinition? IConsumerDefinition.EndpointDefinition => EndpointDefinition;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Message<TMessage>();
    }
}
