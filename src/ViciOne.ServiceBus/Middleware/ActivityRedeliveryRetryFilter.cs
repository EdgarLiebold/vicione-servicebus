using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Schedules redelivery for a transport-independent activity pipeline.</summary>
/// <typeparam name="TContext">The activity execution context carried through the pipeline.</typeparam>
internal sealed class ActivityRedeliveryRetryFilter<TContext> :
    IFilter<TContext>
    where TContext : class, Advanced.ActivityContext
{
    readonly RetryObservable _observers;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a filter that schedules activity redelivery after handled failures.</summary>
    /// <param name="retryPolicy">The policy that classifies activity failures and schedules redelivery.</param>
    /// <param name="observers">The observable that publishes retry lifecycle events.</param>
    public ActivityRedeliveryRetryFilter(IRetryPolicy retryPolicy, RetryObservable observers)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("retry");
        scope.Add("type", "activityRedelivery");
        _retryPolicy.Probe(scope);
    }

    /// <inheritdoc />
    [DebuggerNonUserCode]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return RedeliveryRetryExecution.ExecuteAsync(context, next, _retryPolicy, _observers,
            () => context.GetRedeliveryCount(),
            (retryContext, exception, cancellationToken) => RedeliveryRetryExecution.ScheduleAsync(context, retryContext, exception,
                token => context.NotifyActivityConsumedAsync(context.ReceiveContext.ElapsedTime,
                    TypeCache<ActivityRedeliveryRetryFilter<TContext>>.ShortName, token), cancellationToken));
    }
}
