namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Represents a receiver's connection to the in-memory topology.</summary>
internal interface ITopologyHandle
{
    /// <summary>Gets the identifier assigned to the connection.</summary>
    long Id { get; }

    /// <summary>Disconnects the receiver from the topology.</summary>
    void Disconnect();
}
