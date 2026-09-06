namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by consumer test harness.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Gets the consumed.</summary>
    IReceivedMessageList Consumed { get; }
}
