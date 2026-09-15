using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

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
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        using RetryPolicyContext<TContext> policyContext = _retryPolicy.CreatePolicyContext(context)
            ?? throw new InvalidOperationException("The retry policy returned a null policy context.");
        if (policyContext.Context == null)
            throw new InvalidOperationException("The retry policy returned a policy context without a pipe context.");

        if (_observers.Count > 0)
            await _observers.PostCreateAsync(policyContext).ConfigureAwait(false);

        try
        {
            await next.SendAsync(policyContext.Context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        catch (OperationCanceledException exception)
            when (exception.CancellationToken.IsCancellationRequested
                && exception.CancellationToken == policyContext.Context.CancellationToken)
        {
            throw;
        }
        catch (Exception exception)
        {
            policyContext.Context.CancellationToken.ThrowIfCancellationRequested();

            if (!policyContext.CanRetry(exception, out RetryContext<TContext> retryContext))
            {
                EnsureRetryContext(retryContext);

                if (_retryPolicy.IsHandled(exception))
                {
                    context.GetOrAddPayload(() => retryContext);
                    await retryContext.RetryFaultedAsync(exception).ConfigureAwait(false);

                    if (_observers.Count > 0)
                        await _observers.RetryFaultAsync(retryContext).ConfigureAwait(false);
                }

                throw;
            }

            int previousDeliveryCount = context.Advanced().GetRedeliveryCount();
            for (var retryIndex = 0; retryIndex < previousDeliveryCount; retryIndex++)
            {
                if (retryContext.CanRetry(exception, out RetryContext<TContext> nextRetryContext))
                {
                    EnsureRetryContext(nextRetryContext);
                    retryContext = nextRetryContext;
                    continue;
                }

                EnsureRetryContext(nextRetryContext);
                retryContext = nextRetryContext;

                if (_retryPolicy.IsHandled(exception))
                {
                    context.GetOrAddPayload(() => retryContext);
                    await retryContext.RetryFaultedAsync(exception).ConfigureAwait(false);

                    if (_observers.Count > 0)
                        await _observers.RetryFaultAsync(retryContext).ConfigureAwait(false);
                }

                throw;
            }

            if (_observers.Count > 0)
                await _observers.PostFaultAsync(retryContext).ConfigureAwait(false);

            try
            {
                MessageRedeliveryContext redeliveryContext = context.GetPayload<MessageRedeliveryContext>();
                await redeliveryContext.ScheduleRedeliveryAsync(retryContext.Delay ?? TimeSpan.Zero).ConfigureAwait(false);
                await context.NotifyActivityConsumedAsync(context.Advanced().ReceiveContext.ElapsedTime,
                    TypeCache<ActivityRedeliveryRetryFilter<TContext>>.ShortName).ConfigureAwait(false);
            }
            catch (Exception redeliveryException)
            {
                throw new TransportException(context.Advanced().ReceiveContext.InputAddress,
                    "The message delivery could not be rescheduled", new AggregateException(redeliveryException, exception));
            }
        }
    }

    static void EnsureRetryContext(RetryContext<TContext> retryContext)
    {
        if (retryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
    }
}
