using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Invokes a compensation activity and reports its lifecycle to connected observers.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityFilter<TActivity, TLog> :
    IFilter<CompensateActivityContext<TActivity, TLog>>
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    readonly ActivityObservable _observers;

    /// <summary>Creates an activity invocation filter backed by the shared observer collection.</summary>
    /// <param name="observers">The observers notified before, after, or upon failure of compensation.</param>
    public CompensateActivityFilter(ActivityObservable observers)
    {
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("compensate");
    }

    /// <summary>Invokes compensation, rejects a failed result, and continues the activity-bound pipeline.</summary>
    /// <param name="context">The resolved compensation activity and its deserialized log.</param>
    /// <param name="next">The activity-bound pipeline invoked after successful compensation.</param>
    /// <returns>A task that completes after observer notification and pipeline continuation.</returns>
    public async Task SendAsync(CompensateActivityContext<TActivity, TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            if (_observers.Count > 0)
                await _observers.PreCompensateAsync(context).ConfigureAwait(false);

            var result = context.Result = await context.Activity.CompensateAsync(context).ConfigureAwait(false)
                ?? context.Failed(new ActivityCompensationException("The activity compensation did not return a result"));

            if (result.IsFailed(out var exception))
                exception.Rethrow();

            await next.SendAsync(context).ConfigureAwait(false);

            if (_observers.Count > 0)
                await _observers.PostCompensateAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (context.Result == null || !context.Result.IsFailed(out var faultException) || faultException != exception)
                context.Result = context.Failed(exception);

            if (_observers.Count > 0)
            {
                try
                {
                    await _observers.CompensateFailAsync(context, exception).ConfigureAwait(false);
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
