namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Controls the lifetime of topology.</summary>
public interface TopologyHandle
{
    /// <summary>Gets the id.</summary>
    long Id { get; }

    /// <summary>Disconnects the current observer or endpoint.</summary>
    void Disconnect();
}
