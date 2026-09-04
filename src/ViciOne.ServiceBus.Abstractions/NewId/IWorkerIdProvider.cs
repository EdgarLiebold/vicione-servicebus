namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for worker id provider.
/// </summary>
public interface IWorkerIdProvider
{
    /// <summary>
    /// Gets worker id.
    /// </summary>
    /// <param name="index">The index value.</param>
    /// <returns>The result of the operation.</returns>
    byte[] GetWorkerId(int index);
}
