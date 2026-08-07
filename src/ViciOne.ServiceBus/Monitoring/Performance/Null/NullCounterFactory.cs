// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance.Null
{
    public class NullCounterFactory : ICounterFactory
    {
        public IPerformanceCounter Create(CounterCategory category, string counterName, string instanceName)
        {
            return new NullPerformanceCounter();
        }
    }
}
