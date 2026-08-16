namespace ViciOne.ServiceBus.SignalR.Utils
{
    public interface IViciOneServiceBusFeature
    {
        ConcurrentHashSet<string> Groups { get; }
    }
}
