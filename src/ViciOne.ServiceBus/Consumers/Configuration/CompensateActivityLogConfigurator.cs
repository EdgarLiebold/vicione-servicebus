// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public class CompensateActivityLogConfigurator<TActivity, TLog> :
        ICompensateActivityLogConfigurator<TLog>
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        readonly IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> _configurator;

        public CompensateActivityLogConfigurator(IPipeConfigurator<CompensateActivityContext<TActivity, TLog>> configurator)
        {
            _configurator = configurator;
        }

        public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TLog>> specification)
        {
            _configurator.AddPipeSpecification(specification);
        }
    }
}
