using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for activity.</summary>
public class ActivityObservable :
    Connectable<IActivityObserver>,
    IActivityObserver
{
    /// <summary>Runs before execute.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class
    {
        return ForEachAsync(x => x.PreExecuteAsync(context));
    }

    /// <summary>Runs after execute.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
        where TActivity : class
        where TArguments : class
    {
        return ForEachAsync(x => x.PostExecuteAsync(context));
    }

    /// <summary>Executes fault.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
        where TActivity : class
        where TArguments : class
    {
        return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
    }

    /// <summary>Runs before compensate.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class
    {
        return ForEachAsync(x => x.PreCompensateAsync(context));
    }

    /// <summary>Runs after compensate.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
        where TActivity : class
        where TLog : class
    {
        return ForEachAsync(x => x.PostCompensateAsync(context));
    }

    /// <summary>Compensates fail.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
        where TActivity : class
        where TLog : class
    {
        return ForEachAsync(x => x.CompensateFailAsync(context, exception));
    }
}
