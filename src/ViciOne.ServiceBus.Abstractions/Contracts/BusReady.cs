namespace ViciOne.ServiceBus;

public interface BusReady
{
    IBus Bus { get; }

    HostReady Host { get; }
}
