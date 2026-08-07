// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance.Null
{
    public class NullPerformanceCounter :
        IPerformanceCounter
    {
        public void Increment()
        {
        }

        public void IncrementBy(long val)
        {
        }

        public void Set(long val)
        {
        }

        public void Dispose()
        {
        }
    }
}
