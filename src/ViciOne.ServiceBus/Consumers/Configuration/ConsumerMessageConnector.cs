using System;
using System.Reflection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects consumer message to the service bus pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerMessageConnector<TConsumer, TMessage> :
    IConsumerMessageConnector<TConsumer>
    where TConsumer : class
    where TMessage : class
{
    const ConnectPipeOptions NotConfigureConsumeTopology = ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology;
    readonly IFilter<ConsumerConsumeContext<TConsumer, TMessage>> _consumeFilter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    public ConsumerMessageConnector(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        _consumeFilter = consumeFilter;

        var attribute = typeof(TMessage).GetCustomAttribute<ConfigureConsumeTopologyAttribute>();
        if (attribute != null)
            ConfigureConsumeTopology = attribute.ConfigureConsumeTopology;
    }

    bool ConfigureConsumeTopology { get; } = true;

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new ConsumerMessageSpecification<TConsumer, TMessage>();
    }

    /// <summary>Connects consumer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        IConsumerMessageSpecification<TConsumer, TMessage> messageSpecification = specification.GetMessageSpecification<TMessage>();

        IPipe<ConsumerConsumeContext<TConsumer, TMessage>> consumerPipe = messageSpecification.Build(_consumeFilter);

        IPipe<ConsumeContext<TMessage>> messagePipe = messageSpecification.BuildMessagePipe(x =>
        {
            specification.ConfigureMessagePipe(x);

            x.UseFilter(new ConsumerMessageFilter<TConsumer, TMessage>(consumerFactory, consumerPipe));
        });

        return ConfigureConsumeTopology
            ? consumePipe.ConnectConsumePipe(messagePipe)
            : consumePipe.ConnectConsumePipe(messagePipe, NotConfigureConsumeTopology);
    }
}
