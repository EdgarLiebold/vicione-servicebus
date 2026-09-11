using System;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumers.Contexts;

/// <summary>Exposes the consumer instance that is handling a typed consumed message.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class ConsumerConsumeContextProxy<TConsumer, TMessage> :
    ConsumeContextProxy<TMessage>,
    ConsumerConsumeContext<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    /// <summary>Creates a context that exposes the consumer handling the message.</summary>
    /// <param name="context">The message consume context.</param>
    /// <param name="consumer">The consumer instance.</param>
    public ConsumerConsumeContextProxy(ConsumeContext<TMessage> context, TConsumer consumer)
        : base(context)
    {
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    /// <inheritdoc />
    public TConsumer Consumer { get; }
}
