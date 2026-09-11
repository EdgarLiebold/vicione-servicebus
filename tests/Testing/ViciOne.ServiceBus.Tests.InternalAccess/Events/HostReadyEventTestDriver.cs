using ViciOne.ServiceBus.Events.Readiness;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Events;

public static class HostReadyEventTestDriver
{
    public static HostReady Create(
        Uri address,
        IEnumerable<ReceiveEndpointReady> receiveEndpoints,
        IEnumerable<RiderReady> riders) =>
        new HostReadyEvent(address, receiveEndpoints, riders);
}
