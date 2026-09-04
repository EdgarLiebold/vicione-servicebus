using System;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a message handler consumer definition implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageHandlerConsumerDefinition<TConsumer, TMessage> :
    IConsumerDefinition<TConsumer>
    where TMessage : class
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => default;
    /// <summary>
    /// Gets the consumer type value.
    /// </summary>
    public Type ConsumerType => typeof(TConsumer);

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="endpointConfigurator">The endpoint configurator value.</param>
    /// <param name="consumerConfigurator">The consumer configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
    }

    /// <summary>
    /// Gets or sets the endpoint definition value.
    /// </summary>
    public IEndpointDefinition<TConsumer>? EndpointDefinition { get; set; }

    IEndpointDefinition? IConsumerDefinition.EndpointDefinition => EndpointDefinition;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Message<TMessage>();
    }
}
