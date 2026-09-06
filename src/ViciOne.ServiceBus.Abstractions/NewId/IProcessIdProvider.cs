namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides process id services.</summary>
public interface IProcessIdProvider
{
    /// <summary>Gets process id.</summary>
    /// <returns>The process id.</returns>
    byte[] GetProcessId();
}
