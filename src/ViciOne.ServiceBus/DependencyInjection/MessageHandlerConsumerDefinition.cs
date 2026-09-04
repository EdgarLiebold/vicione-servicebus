using System;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection;

public class MessageHandlerConsumerDefinition<TConsumer, TMessage> :
    IConsumerDefinition<TConsumer>
    where TMessage : class
    where TConsumer : class, IConsumer
{
    public int? ConcurrentMessageLimit => default;
    public Type ConsumerType => typeof(TConsumer);

    public void Configure(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
    }

    public IEndpointDefinition<TConsumer>? EndpointDefinition { get; set; }

    IEndpointDefinition? IConsumerDefinition.EndpointDefinition => EndpointDefinition;

    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        return EndpointDefinition?.GetEndpointName(formatter) ?? formatter.Message<TMessage>();
    }
}
