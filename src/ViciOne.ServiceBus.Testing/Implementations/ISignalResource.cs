namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Represents a resource which may be signaled.</summary>
public interface ISignalResource
{
    /// <summary>Signals the configured condition.</summary>
    void Signal();
}
