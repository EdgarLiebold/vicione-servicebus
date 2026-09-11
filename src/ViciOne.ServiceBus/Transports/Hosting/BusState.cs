namespace ViciOne.ServiceBus.Transports;

/// <summary>Identifies the current lifecycle state of a bus instance.</summary>
public enum BusState
{
    /// <summary>The bus has been created but has not started.</summary>
    Created = 0,
    /// <summary>The bus is running.</summary>
    Started = 1,
    /// <summary>The bus stopped making progress because of a failure.</summary>
    Faulted = 2,
    /// <summary>The bus has completed shutdown.</summary>
    Stopped = 3,
}
