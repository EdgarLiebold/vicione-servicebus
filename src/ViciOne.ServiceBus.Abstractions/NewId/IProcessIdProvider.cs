namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for process id provider.
/// </summary>
public interface IProcessIdProvider
{
    /// <summary>
    /// Gets process id.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    byte[] GetProcessId();
}
