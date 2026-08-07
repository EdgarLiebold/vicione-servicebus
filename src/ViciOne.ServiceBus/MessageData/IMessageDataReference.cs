// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData
{
    public interface IMessageDataReference
    {
        string Text { set; }
        byte[] Data { set; }
    }
}
