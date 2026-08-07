// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IInMemoryPublishTopology :
        IPublishTopology
    {
        new IInMemoryMessagePublishTopology<T> GetMessageTopology<T>()
            where T : class;
    }
}
