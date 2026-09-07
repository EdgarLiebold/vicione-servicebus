namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface IActivityFactory<out TActivity, TArguments, TLog> :
    IExecuteActivityFactory<TActivity, TArguments>,
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
}
