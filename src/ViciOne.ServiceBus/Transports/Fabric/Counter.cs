// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports.Fabric
{
    using System.Threading;


    public class Counter :
        Metric
    {
        long _count;

        public void Add()
        {
            Interlocked.Increment(ref _count);
        }
    }
}
