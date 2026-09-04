using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides an activity observable implementation.
/// </summary>
public class ActivityObservable :
    Connectable<IActivityObserver>,
    IActivityObserver
{
    /// <summary>
    /// Performs the pre execute operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.PreExecuteAsync(context));
    }

    /// <summary>
    /// Performs the post execute operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.PostExecuteAsync(context));
    }

    /// <summary>
    /// Performs the execute fault operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
    }

    /// <summary>
    /// Performs the pre compensate operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.PreCompensateAsync(context));
    }

    /// <summary>
    /// Performs the post compensate operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.PostCompensateAsync(context));
    }

    /// <summary>
    /// Performs the compensate fail operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        return ForEachAsync(x => x.CompensateFailAsync(context, exception));
    }
}
