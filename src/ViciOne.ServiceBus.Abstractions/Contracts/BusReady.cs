namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by bus ready.</summary>
public interface BusReady
{
    /// <summary>Gets the bus.</summary>
    IBus Bus { get; }

    /// <summary>Gets the host.</summary>
    HostReady Host { get; }
}
