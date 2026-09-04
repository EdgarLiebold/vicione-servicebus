namespace ViciOne.ServiceBus;

public interface ReceiveEndpointStopping :
    ReceiveEndpointEvent
{
    bool Removed { get; }
}
