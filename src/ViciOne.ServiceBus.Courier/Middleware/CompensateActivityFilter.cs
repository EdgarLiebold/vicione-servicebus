using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Compensates an activity as part of an activity execute host pipe.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityFilter<TActivity, TLog> :
    IFilter<CompensateActivityContext<TActivity, TLog>>
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    readonly ActivityObservable _observers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="observers">The observers.</param>
    public CompensateActivityFilter(ActivityObservable observers)
    {
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("compensate");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
                await _observers.CompensateFailAsync(context, exception).ConfigureAwait(false);

            throw;
        }
    }
}
