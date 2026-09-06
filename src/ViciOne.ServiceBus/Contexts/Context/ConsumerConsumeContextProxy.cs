namespace ViciOne.ServiceBus.Context;

/// <summary>A consumer instance merged with a message consume context.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerConsumeContextProxy<TConsumer, TMessage> :
    ConsumeContextProxy<TMessage>,
    ConsumerConsumeContext<TConsumer, TMessage>
    where TMessage : class
    where TConsumer : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumer">The consumer.</param>
    public ConsumerConsumeContextProxy(ConsumeContext<TMessage> context, TConsumer consumer)
        : base(context)
    {
        Consumer = consumer;
    }

    /// <summary>Gets the consumer.</summary>
    public TConsumer Consumer { get; }
}
