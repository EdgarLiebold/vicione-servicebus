namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Represents a resource which may be signaled.
/// </summary>
public interface ISignalResource
{
    /// <summary>
    /// Performs the signal operation.
    /// </summary>
    void Signal();
}
