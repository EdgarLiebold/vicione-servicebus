// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Configure the execution of the activity and arguments with some tasty middleware.
    /// </summary>
    /// <typeparam name="TLog"></typeparam>
    public interface ICompensateLogConfigurator<TLog> :
        IPipeConfigurator<CompensateContext<TLog>>,
        IConsumeConfigurator
        where TLog : class
    {
    }
}
