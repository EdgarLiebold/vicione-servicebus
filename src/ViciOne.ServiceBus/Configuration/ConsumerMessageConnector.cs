using System;
using System.Reflection;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds and connects the pipelines for one consumer and message contract.</summary>
/// <typeparam name="TConsumer">The consumer implementation invoked by the pipeline.</typeparam>
/// <typeparam name="TMessage">The message contract accepted by the pipeline.</typeparam>
public sealed class ConsumerMessageConnector<TConsumer, TMessage> :
    IConsumerMessageConnector<TConsumer>
    where TConsumer : class
    where TMessage : class
{
    const ConnectPipeOptions NotConfigureConsumeTopology = ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology;
    readonly IFilter<ConsumerConsumeContext<TConsumer, TMessage>> _consumeFilter;

    /// <summary>Creates a connector around the filter that invokes the consumer method.</summary>
    /// <param name="consumeFilter">The terminal consumer invocation filter.</param>
    public ConsumerMessageConnector(IFilter<ConsumerConsumeContext<TConsumer, TMessage>> consumeFilter)
    {
        _consumeFilter = consumeFilter ?? throw new ArgumentNullException(nameof(consumeFilter));

        var attribute = typeof(TMessage).GetCustomAttribute<ConfigureConsumeTopologyAttribute>();
        if (attribute != null)
            ConfigureConsumeTopology = attribute.ConfigureConsumeTopology;
    }

    bool ConfigureConsumeTopology { get; } = true;

    /// <summary>Gets the message contract accepted by this connector.</summary>
    public Type MessageType => typeof(TMessage);

    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new ConsumerMessageSpecification<TConsumer, TMessage>();
    }

    /// <summary>Connects consumer.</summary>
    /// <param name="consumePipe">The consume pipe that will dispatch matching messages.</param>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(specification);

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
