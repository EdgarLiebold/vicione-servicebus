namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a consumer instance together with the typed message context it is processing.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface ConsumerConsumeContext<out TConsumer, out TMessage> :
    ConsumerConsumeContext<TConsumer>,
    ConsumeContext<TMessage>
    where TMessage : class
    where TConsumer : class
{
}


/// <summary>Provides a consumer instance together with its untyped consume context.</summary>
/// <typeparam name="TConsumer">The consumer implementation exposed by the context.</typeparam>
public interface ConsumerConsumeContext<out TConsumer> :
    ConsumeContext
    where TConsumer : class
{
    /// <summary>Gets the consumer instance that handles the message.</summary>
    TConsumer Consumer { get; }
}
