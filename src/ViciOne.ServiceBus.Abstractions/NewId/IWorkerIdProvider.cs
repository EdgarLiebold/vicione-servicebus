namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides worker id services.</summary>
public interface IWorkerIdProvider
{
    /// <summary>Gets worker id.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The worker id.</returns>
    byte[] GetWorkerId(int index);
}
