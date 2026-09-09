namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the middleware pipeline that processes an activity's compensation log.</summary>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface ICompensateLogConfigurator<TLog> :
    IPipeConfigurator<CompensateContext<TLog>>,
    IConsumeConfigurator
    where TLog : class
{
}
