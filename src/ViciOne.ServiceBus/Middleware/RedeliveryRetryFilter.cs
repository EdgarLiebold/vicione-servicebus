using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

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
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        using (RetryPolicyContext<TContext> policyContext = _retryPolicy.CreatePolicyContext(context)
            ?? throw new InvalidOperationException("The retry policy returned a null policy context."))
        {
            if (policyContext.Context == null)
                throw new InvalidOperationException("The retry policy returned a policy context without a pipe context.");

            if (_observers.Count > 0)
            {
                var postCreateTask = _observers.PostCreateAsync(policyContext);
                if (postCreateTask.Status != TaskStatus.RanToCompletion)
                    await postCreateTask.ConfigureAwait(false);
            }

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

                        var retryFaultedTask = retryContext.RetryFaultedAsync(exception);
                        if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                            await retryFaultedTask.ConfigureAwait(false);

                        if (_observers.Count > 0)
                        {
                            var retryFaultTask = _observers.RetryFaultAsync(retryContext);
                            if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultTask.ConfigureAwait(false);
                        }
                    }

                    throw;
                }

                var previousDeliveryCount = context.Advanced().GetRedeliveryCount();
                for (var retryIndex = 0; retryIndex < previousDeliveryCount; retryIndex++)
                {
                    if (!retryContext.CanRetry(exception, out retryContext))
                    {
                        EnsureRetryContext(retryContext);

                        if (_retryPolicy.IsHandled(exception))
                        {
                            context.GetOrAddPayload(() => retryContext);

                            var retryFaultedTask = retryContext.RetryFaultedAsync(exception);
                            if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultedTask.ConfigureAwait(false);

                            if (_observers.Count > 0)
                            {
                                var retryFaultTask = _observers.RetryFaultAsync(retryContext);
                                if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                    await retryFaultTask.ConfigureAwait(false);
                            }
                        }

                        throw;
                    }
                }

                EnsureRetryContext(retryContext);

                if (_observers.Count > 0)
                {
                    var postFaultTask = _observers.PostFaultAsync(retryContext);
                    if (postFaultTask.Status != TaskStatus.RanToCompletion)
                        await postFaultTask.ConfigureAwait(false);
                }

                try
                {
                    var redeliveryContext = context.GetPayload<MessageRedeliveryContext>();

                    var delay = retryContext.Delay ?? TimeSpan.Zero;

                    await redeliveryContext.ScheduleRedeliveryAsync(delay).ConfigureAwait(false);

                    await context.NotifyConsumedAsync(context, context.Advanced().ReceiveContext.ElapsedTime,
                        TypeCache<RedeliveryRetryFilter<TContext, TMessage>>.ShortName).ConfigureAwait(false);
                }
                catch (Exception redeliveryException)
                {
                    throw new TransportException(context.Advanced().ReceiveContext.InputAddress, "The message delivery could not be rescheduled",
                        new AggregateException(redeliveryException, exception));
                }
            }
        }
    }

    static void EnsureRetryContext(RetryContext<TContext> retryContext)
    {
        if (retryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
    }
}
