namespace ViciOne.ServiceBus.Events;

/// <summary>Pairs a ready bus instance with its host-readiness snapshot.</summary>
internal sealed class BusReadyEvent :
    BusReady
{
    /// <summary>Creates a bus-readiness notification.</summary>
    /// <param name="host">The ready host snapshot.</param>
    /// <param name="bus">The bus that reached readiness.</param>
    public BusReadyEvent(HostReady host, IBus bus)
    {
        Host = host ?? throw new System.ArgumentNullException(nameof(host));
        Bus = bus ?? throw new System.ArgumentNullException(nameof(bus));
    }

    /// <summary>Gets the bus that reached readiness.</summary>
    public IBus Bus { get; }

    /// <summary>Gets the associated host-readiness snapshot.</summary>
    public HostReady Host { get; }
}
