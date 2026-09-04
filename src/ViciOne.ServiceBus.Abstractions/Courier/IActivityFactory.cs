using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Defines the contract for activity factory.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface IActivityFactory<out TActivity, TArguments, TLog> :
    IExecuteActivityFactory<TActivity, TArguments>,
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, IExecuteActivity<TArguments>, ICompensateActivity<TLog>
    where TArguments : class
    where TLog : class
{
}


/// <summary>
/// Should be implemented by containers that support generic object resolution in order to
/// provide a common lifetime management policy for all activities
/// </summary>
public interface IActivityFactory :
    IProbeSite
{
    /// <summary>
    /// Create and execute the activity
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <typeparam name="TArguments"></typeparam>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ExecuteAsync<TActivity, TArguments>(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>
    /// Create and compensate the activity
    /// </summary>
    /// <param name="compensateContext"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CompensateAsync<TActivity, TLog>(CompensateContext<TLog> compensateContext, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class;
}
