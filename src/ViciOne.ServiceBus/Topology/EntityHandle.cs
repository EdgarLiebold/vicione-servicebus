namespace ViciOne.ServiceBus.Topology;

/// <summary>Identifies a broker-topology entity within its owning topology builder.</summary>
public interface EntityHandle
{
    /// <summary>Gets the identifier assigned by the topology builder.</summary>
    long Id { get; }
}
