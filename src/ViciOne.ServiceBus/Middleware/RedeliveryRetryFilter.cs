using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Schedules broker redelivery for handled failures instead of holding the current delivery.</summary>
/// <typeparam name="TContext">The consume context type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
internal sealed class RedeliveryRetryFilter<TContext, TMessage> :
    IFilter<TContext>
    where TContext : class, ConsumeContext<TMessage>
    where TMessage : class
{
    readonly RetryObservable _observers;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a redelivery filter for a policy and its lifecycle observers.</summary>
    /// <param name="retryPolicy">The policy that selects and schedules redelivery attempts.</param>
    /// <param name="observers">The observable that publishes retry lifecycle events.</param>
    public RedeliveryRetryFilter(IRetryPolicy retryPolicy, RetryObservable observers)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("retry");
        scope.Add("type", "redelivery");

        _retryPolicy.Probe(scope);
    }

    /// <summary>Invokes the pipeline and schedules redelivery when a handled failure permits another attempt.</summary>
    /// <param name="context">The current message delivery.</param>
    /// <param name="next">The next consume-pipeline stage.</param>
    /// <returns>A task that completes after delivery, terminal failure, or redelivery scheduling.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return RedeliveryRetryExecution.ExecuteAsync(context, next, _retryPolicy, _observers,
            () => context.Advanced().GetRedeliveryCount(),
            (retryContext, exception, cancellationToken) => RedeliveryRetryExecution.ScheduleAsync(context.Advanced(), retryContext, exception,
                token => context.NotifyConsumedAsync(context, context.Advanced().ReceiveContext.ElapsedTime,
                    TypeCache<RedeliveryRetryFilter<TContext, TMessage>>.ShortName, token), cancellationToken));
    }
}
