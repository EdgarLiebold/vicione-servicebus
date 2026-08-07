// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Metadata
{
    public interface IImplementedMessageType
    {
        void ImplementsMessageType<T>(bool direct)
            where T : class;
    }
}
