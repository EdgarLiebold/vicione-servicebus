namespace ViciOne.ServiceBus.Topology;

/// <summary>Controls the lifetime of entity.</summary>
public interface EntityHandle
{
    /// <summary>Gets the id.</summary>
    long Id { get; }
}
