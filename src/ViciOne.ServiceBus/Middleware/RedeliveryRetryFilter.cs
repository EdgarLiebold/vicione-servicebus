using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Uses the message redelivery mechanism, if available, to delay a retry without blocking message delivery.</summary>
/// <typeparam name="TContext">The context type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class RedeliveryRetryFilter<TContext, TMessage> :
    IFilter<TContext>
    where TContext : class, ConsumeContext<TMessage>
    where TMessage : class
{
    readonly RetryObservable _observers;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="observers">The observers.</param>
    public RedeliveryRetryFilter(IRetryPolicy retryPolicy, RetryObservable observers)
    {
        _retryPolicy = retryPolicy;
        _observers = observers;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("retry");
        scope.Add("type", "redelivery");

        _retryPolicy.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        using (RetryPolicyContext<TContext> policyContext = _retryPolicy.CreatePolicyContext(context))
        {
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
}
