// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData
{
    public interface IInlineMessageData
    {
        void Set(IMessageDataReference reference);
    }
}
