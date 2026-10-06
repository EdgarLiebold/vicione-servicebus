using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Invokes an execution activity and reports its lifecycle to connected observers.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityFilter<TActivity, TArguments> :
    IFilter<ExecuteActivityContext<TActivity, TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly ActivityObservable _observers;

    /// <summary>Creates an activity invocation filter backed by the shared observer collection.</summary>
    /// <param name="observers">The observers notified before, after, or upon failure of execution.</param>
    public ExecuteActivityFilter(ActivityObservable observers)
    {
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("execute");
    }

    /// <summary>Invokes execution, rejects a faulted result, and continues the activity-bound pipeline.</summary>
    /// <param name="context">The resolved execution activity and its deserialized arguments.</param>
    /// <param name="next">The activity-bound pipeline invoked after successful execution.</param>
    /// <returns>A task that completes after observer notification and pipeline continuation.</returns>
    public async Task SendAsync(ExecuteActivityContext<TActivity, TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            if (_observers.Count > 0)
                await _observers.PreExecuteAsync(context).ConfigureAwait(false);

            var result = context.Result = await context.Activity.ExecuteAsync(context).ConfigureAwait(false)
                ?? context.Faulted(new ActivityExecutionException("The activity execute did not return a result"));

            if (result.IsFaulted(out var exception))
                exception.Rethrow();

            await next.SendAsync(context).ConfigureAwait(false);

            if (_observers.Count > 0)
                await _observers.PostExecuteAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (context.Result == null || !context.Result.IsFaulted(out var faultException) || faultException != exception)
                context.Result = context.Faulted(exception);

            if (_observers.Count > 0)
            {
                try
                {
                    await _observers.ExecuteFaultAsync(context, exception).ConfigureAwait(false);
                }
                catch (Exception observerException)
                {
                    throw new AggregateException(exception, observerException);
                }
            }

            throw;
        }
    }
}
