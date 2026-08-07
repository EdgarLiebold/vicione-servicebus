// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface ReceiveEndpointStopping :
        ReceiveEndpointEvent
    {
        bool Removed { get; }
    }
}
