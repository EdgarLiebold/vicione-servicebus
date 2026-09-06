namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// A consumer and consume context mixed together, carrying both a consumer and the message
/// consume context.
/// </summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface ConsumerConsumeContext<out TConsumer, out TMessage> :
    ConsumerConsumeContext<TConsumer>,
    ConsumeContext<TMessage>
    where TMessage : class
    where TConsumer : class
{
}


/// <summary>Exposes state for consumer consume operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface ConsumerConsumeContext<out TConsumer> :
    ConsumeContext
    where TConsumer : class
{
    /// <summary>The consumer which will handle the message.</summary>
    TConsumer Consumer { get; }
}
