namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configure the execution of the activity and arguments with some tasty middleware.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityLogConfigurator<TLog> :
    IPipeConfigurator<CompensateActivityContext<TLog>>,
    IConsumeConfigurator
    where TLog : class
{
}
