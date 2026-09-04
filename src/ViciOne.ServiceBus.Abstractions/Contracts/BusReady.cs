namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for bus ready.
/// </summary>
public interface BusReady
{
    /// <summary>
    /// Gets the bus value.
    /// </summary>
    IBus Bus { get; }

    /// <summary>
    /// Gets the host value.
    /// </summary>
    HostReady Host { get; }
}
