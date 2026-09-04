namespace ViciOne.ServiceBus.Testing;

public interface IConsumerTestHarness<TConsumer>
    where TConsumer : class, IConsumer
{
    IReceivedMessageList Consumed { get; }
}
