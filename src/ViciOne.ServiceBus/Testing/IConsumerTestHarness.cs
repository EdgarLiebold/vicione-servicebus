// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
{
    public interface IConsumerTestHarness<TConsumer>
        where TConsumer : class, IConsumer
    {
        IReceivedMessageList Consumed { get; }
    }
}
