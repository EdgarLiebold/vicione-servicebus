namespace ViciOne.ServiceBus.Context;

/// <summary>
/// A consumer instance merged with a message consume context
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class ConsumerConsumeContextScope<TConsumer, TMessage> :
    ConsumeContextScope<TMessage>,
    ConsumerConsumeContext<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="consumer">The consumer value.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer)
        : base(context)
    {
        Consumer = consumer;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="consumer">The consumer value.</param>
    /// <param name="payloads">The payloads value.</param>
    public ConsumerConsumeContextScope(ConsumeContext<TMessage> context, TConsumer consumer, params object[] payloads)
        : base(context, payloads)
    {
        Consumer = consumer;
    }

    /// <summary>
    /// Gets the consumer value.
    /// </summary>
    public TConsumer Consumer { get; }
}
