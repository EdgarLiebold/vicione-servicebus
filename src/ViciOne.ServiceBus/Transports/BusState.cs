namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Specifies the available bus state values.
/// </summary>
public enum BusState
{
    /// <summary>
    /// Indicates created.
    /// </summary>
    Created = 0,
    /// <summary>
    /// Indicates started.
    /// </summary>
    Started = 1,
    /// <summary>
    /// Indicates faulted.
    /// </summary>
    Faulted = 2,
    /// <summary>
    /// Indicates stopped.
    /// </summary>
    Stopped = 3,
}
