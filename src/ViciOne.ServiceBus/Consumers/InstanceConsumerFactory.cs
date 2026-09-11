using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers.Contexts;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumers;

/// <summary>Invokes each delivery on a caller-owned consumer instance without taking ownership of its lifetime.</summary>
/// <typeparam name="TConsumer">The consumer implementation supplied by the caller.</typeparam>
public sealed class InstanceConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly TConsumer _consumer;

    /// <summary>Creates a factory over a caller-owned consumer instance.</summary>
    /// <param name="consumer">The consumer used for every delivery.</param>
    public InstanceConsumerFactory(TConsumer consumer)
    {
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    /// <summary>Invokes the message pipeline with the caller-owned consumer instance.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="context">The received message and its consume context.</param>
    /// <param name="next">The consumer pipeline to invoke with the supplied instance.</param>
    /// <returns>A task that completes after the consumer pipeline finishes.</returns>
    public Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return next.SendAsync(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, _consumer));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateConsumerFactoryScope<TConsumer>("instance");
    }
}
