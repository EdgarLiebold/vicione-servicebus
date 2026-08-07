// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface ConsumerFaultContext
    {
        string MessageType { get; }
        string ConsumerType { get; }
    }
}
