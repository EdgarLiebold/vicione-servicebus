namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the bus ready event data.</summary>
public class BusReadyEvent :
    BusReady
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host.</param>
    /// <param name="bus">The bus.</param>
    public BusReadyEvent(HostReady host, IBus bus)
    {
        Host = host;
        Bus = bus;
    }

    /// <summary>Gets the bus.</summary>
    public IBus Bus { get; }

    /// <summary>Gets the host.</summary>
    public HostReady Host { get; }
}
