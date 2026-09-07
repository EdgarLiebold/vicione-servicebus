namespace ViciOne.ServiceBus.Courier;

/// <summary>Defines a routing-slip activity that can execute and compensate its completed work.</summary>
/// <typeparam name="TArguments">The activity argument type.</typeparam>
/// <typeparam name="TLog">The activity log argument type.</typeparam>
public interface IActivity<in TArguments, in TLog> :
    IExecuteActivity<TArguments>,
    ICompensateActivity<TLog>
    where TLog : class
    where TArguments : class
{
}
