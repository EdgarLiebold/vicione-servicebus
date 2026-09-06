using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Tracks a cumulative message-fabric measurement.</summary>
public class Counter :
    Metric
{
    long _count;

    /// <summary>Adds the supplied value to the current collection.</summary>
    public void Add()
    {
        Interlocked.Increment(ref _count);
    }
}
