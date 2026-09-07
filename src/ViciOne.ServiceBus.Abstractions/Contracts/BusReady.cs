namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a bus and its host after startup has completed.</summary>
public interface BusReady
{
    /// <summary>Gets the bus that reached readiness.</summary>
    IBus Bus { get; }

    /// <summary>Gets the associated host-readiness snapshot.</summary>
    HostReady Host { get; }
}
