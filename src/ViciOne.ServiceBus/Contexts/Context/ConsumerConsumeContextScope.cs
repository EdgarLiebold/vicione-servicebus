namespace ViciOne.ServiceBus.Context;

/// <summary>A consumer instance merged with a message consume context.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerConsumeContextScope<TConsumer, TMessage> :
    ConsumeContextScope<TMessage>,
    ConsumerConsumeContext<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumer">The consumer.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer)
        : base(context)
    {
        Consumer = consumer;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumer">The consumer.</param>
    /// <param name="payloads">The payloads.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer, params object[] payloads)
        : base(context, payloads)
    {
        Consumer = consumer;
    }

    /// <summary>Gets the consumer.</summary>
    public TConsumer Consumer { get; }
}
