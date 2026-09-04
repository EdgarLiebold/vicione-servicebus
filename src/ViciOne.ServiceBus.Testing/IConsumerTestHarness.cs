namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for consumer test harness.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public interface IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Gets the consumed value.
    /// </summary>
    IReceivedMessageList Consumed { get; }
}
