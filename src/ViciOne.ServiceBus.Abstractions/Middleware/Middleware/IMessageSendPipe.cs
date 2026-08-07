// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public interface IMessageSendPipe<in TMessage> :
        IPipe<SendContext<TMessage>>
        where TMessage : class
    {
    }
}
