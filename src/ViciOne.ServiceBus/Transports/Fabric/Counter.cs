using System.Threading;

namespace ViciOne.ServiceBus.Transports.Fabric;

public class Counter :
    Metric
{
    long _count;

    public void Add()
    {
        Interlocked.Increment(ref _count);
    }
}
