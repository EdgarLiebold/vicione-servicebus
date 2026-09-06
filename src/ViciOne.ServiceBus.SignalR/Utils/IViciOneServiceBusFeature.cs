namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>Defines the operations required by vici one service bus feature.</summary>
public interface IViciOneServiceBusFeature
{
    /// <summary>Gets the groups.</summary>
    ConcurrentHashSet<string> Groups { get; }
}
