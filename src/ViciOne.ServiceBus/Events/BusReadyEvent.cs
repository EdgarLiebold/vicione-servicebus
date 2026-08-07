// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Events
{
    public class BusReadyEvent :
        BusReady
    {
        public BusReadyEvent(HostReady host, IBus bus)
        {
            Host = host;
            Bus = bus;
        }

        public IBus Bus { get; }

        public HostReady Host { get; }
    }
}
