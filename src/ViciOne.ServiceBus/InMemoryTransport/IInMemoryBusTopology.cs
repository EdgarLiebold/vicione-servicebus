// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IInMemoryBusTopology :
        IBusTopology
    {
        new IInMemoryMessagePublishTopology<T> Publish<T>()
            where T : class;
    }
}
