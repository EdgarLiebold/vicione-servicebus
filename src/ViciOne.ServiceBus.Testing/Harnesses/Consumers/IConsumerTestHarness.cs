namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes message deliveries observed for a registered consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
public interface IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Gets successful and faulted messages delivered to the consumer.</summary>
    IConsumedMessageList Consumed { get; }
}
