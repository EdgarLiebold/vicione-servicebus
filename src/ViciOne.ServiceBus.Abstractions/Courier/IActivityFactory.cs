using System.Threading.Tasks;

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


/// <summary>
/// Should be implemented by containers that support generic object resolution in order to
/// provide a common lifetime management policy for all activities.
/// </summary>
public interface IActivityFactory :
    IProbeSite
{
    /// <summary>Create and execute the activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync<TActivity, TArguments>(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Create and compensate the activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="compensateContext">The compensate context.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompensateAsync<TActivity, TLog>(CompensateContext<TLog> compensateContext, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class;
}
