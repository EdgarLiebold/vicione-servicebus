namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware applied to a deserialized compensation log.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityLogConfigurator<TLog> :
    IPipeConfigurator<CompensateActivityContext<TLog>>,
    IConsumeConfigurator
    where TLog : class
{
}
