// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public interface IMessagePublishPipe<in TMessage> :
        IPipe<PublishContext<TMessage>>
        where TMessage : class
    {
    }
}
