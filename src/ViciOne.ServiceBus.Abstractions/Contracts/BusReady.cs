// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface BusReady
    {
        IBus Bus { get; }

        HostReady Host { get; }
    }
}
