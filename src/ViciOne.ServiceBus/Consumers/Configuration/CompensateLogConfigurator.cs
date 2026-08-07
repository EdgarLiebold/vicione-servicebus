// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class CompensateLogConfigurator<TLog> :
        ICompensateLogConfigurator<TLog>
        where TLog : class
    {
        readonly IPipeConfigurator<CompensateContext<TLog>> _configurator;

        public CompensateLogConfigurator(IPipeConfigurator<CompensateContext<TLog>> configurator)
        {
            _configurator = configurator;
        }

        public void AddPipeSpecification(IPipeSpecification<CompensateContext<TLog>> specification)
        {
            _configurator.AddPipeSpecification(specification);
        }
    }
}
