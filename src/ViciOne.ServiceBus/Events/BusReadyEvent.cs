namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a bus ready event implementation.
/// </summary>
public class BusReadyEvent :
    BusReady
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="bus">The bus value.</param>
    public BusReadyEvent(HostReady host, IBus bus)
    {
        Host = host;
        Bus = bus;
    }

    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus { get; }

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostReady Host { get; }
}
