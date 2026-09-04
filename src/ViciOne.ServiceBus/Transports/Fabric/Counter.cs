using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a counter implementation.
/// </summary>
public class Counter :
    Metric
{
    long _count;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    public void Add()
    {
        Interlocked.Increment(ref _count);
    }
}
