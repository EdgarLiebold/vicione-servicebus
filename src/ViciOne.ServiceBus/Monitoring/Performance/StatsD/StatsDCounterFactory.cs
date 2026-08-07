// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance.StatsD
{
    public class StatsDCounterFactory : ICounterFactory
    {
        readonly StatsDConfiguration _config;

        public StatsDCounterFactory(StatsDConfiguration config)
        {
            _config = config;
        }

        public IPerformanceCounter Create(CounterCategory category, string counterName, string instanceName)
        {
            return new StatsDPerformanceCounter(_config, category.Name, counterName, instanceName);
        }
    }
}
