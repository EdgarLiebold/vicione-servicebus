namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a transport rider that completed startup with its host.</summary>
public interface RiderReady
{
    /// <summary>Gets the rider's registration name.</summary>
    string Name { get; }
}
