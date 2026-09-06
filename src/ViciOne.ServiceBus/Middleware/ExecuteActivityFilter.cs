using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes an activity as part of an activity execute host pipe.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityFilter<TActivity, TArguments> :
    IFilter<ExecuteActivityContext<TActivity, TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly ActivityObservable _observers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="observers">The observers.</param>
    public ExecuteActivityFilter(ActivityObservable observers)
    {
        _observers = observers;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("execute");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ExecuteActivityContext<TActivity, TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next)
    {
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
                await _observers.ExecuteFaultAsync(context, exception).ConfigureAwait(false);

            throw;
        }
    }
}
