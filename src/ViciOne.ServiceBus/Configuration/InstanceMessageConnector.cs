using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Connects a consumer instance to the inbound pipeline for the specified message type. The actual
/// filter that invokes the consume method is passed in, so that a consumer interface with a
/// different consume signature can be bound through the same connector.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation invoked by the pipeline.</typeparam>
/// <typeparam name="TMessage">The message contract accepted by the pipeline.</typeparam>
public sealed class InstanceMessageConnector<TConsumer, TMessage> :
    IInstanceMessageConnector<TConsumer>
    where TConsumer : class
    where TMessage : class
{
    readonly IFilter<ConsumerConsumeContext<TConsumer, TMessage>> _consumeFilter;

    /// <summary>Creates an instance connector around the consume-method filter.</summary>
    /// <param name="consumeFilter">The consume method invocation filter.</param>
    public InstanceMessageConnector(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        _consumeFilter = consumeFilter ?? throw new ArgumentNullException(nameof(consumeFilter));
    }

    Type IInstanceMessageConnector.MessageType => typeof(TMessage);

    /// <summary>Connects this message contract for an existing consumer instance.</summary>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, TConsumer instance, IConsumerSpecification<TConsumer> specification)
    {
        ArgumentNullException.ThrowIfNull(pipeConnector);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(specification);

        IConsumerMessageSpecification<TConsumer, TMessage> messageSpecification = specification.GetMessageSpecification<TMessage>();

        IPipe<ConsumerConsumeContext<TConsumer, TMessage>> consumerPipe = messageSpecification.Build(_consumeFilter);

        IPipe<ConsumeContext<TMessage>> messagePipe = messageSpecification.BuildMessagePipe(x =>
        {
            specification.ConfigureMessagePipe(x);

            x.UseFilter(new InstanceMessageFilter<TConsumer, TMessage>(instance, consumerPipe));
        });

        return pipeConnector.ConnectConsumePipe(messagePipe);
    }

    /// <summary>Creates an independent specification for this consumer-message pipeline.</summary>
    /// <returns>A new consumer-message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new ConsumerMessageSpecification<TConsumer, TMessage>();
    }
}
