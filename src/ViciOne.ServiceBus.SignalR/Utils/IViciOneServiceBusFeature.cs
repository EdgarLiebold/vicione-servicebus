namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>
/// Defines the contract for vici one service bus feature.
/// </summary>
public interface IViciOneServiceBusFeature
{
    /// <summary>
    /// Gets the groups value.
    /// </summary>
    ConcurrentHashSet<string> Groups { get; }
}
