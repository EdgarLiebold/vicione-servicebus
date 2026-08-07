// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ContainerTests.Scenarios
{
    public interface AnotherMessageConsumer :
        IConsumer<AnotherMessageInterface>
    {
        AnotherMessageInterface Last { get; }
    }
}
