namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates execution and compensation instances for one routing-slip activity contract.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface IActivityFactory<out TActivity, TArguments, TLog> :
    IExecuteActivityFactory<TActivity, TArguments>,
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
}
