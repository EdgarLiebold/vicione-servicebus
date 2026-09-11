using System;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Consumers.Contexts;

/// <summary>Adds local payloads while exposing the consumer instance that is handling a typed consumed message.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class ConsumerConsumeContextScope<TConsumer, TMessage> :
    ConsumeContextScope<TMessage>,
    ConsumerConsumeContext<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    /// <summary>Creates a scoped context that exposes the consumer handling the message.</summary>
    /// <param name="context">The message consume context.</param>
    /// <param name="consumer">The consumer instance.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer)
        : base(context)
    {
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    /// <summary>Creates a scoped consumer context with additional payloads.</summary>
    /// <param name="context">The message consume context.</param>
    /// <param name="consumer">The consumer instance.</param>
    /// <param name="payloads">The payloads visible within this consumer scope.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer, params object[] payloads)
        : base(context, payloads)
    {
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
    }

    /// <inheritdoc />
    public TConsumer Consumer { get; }
}
