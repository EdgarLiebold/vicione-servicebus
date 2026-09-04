namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for topology handle.
/// </summary>
public interface TopologyHandle
{
    /// <summary>
    /// Gets the id value.
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Performs the disconnect operation.
    /// </summary>
    void Disconnect();
}
