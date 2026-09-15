using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Repeats a pipeline operation according to a retry policy and publishes its lifecycle.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
internal sealed class RetryFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly RetryObservable _observers;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a retry filter for a policy and its lifecycle observers.</summary>
    /// <param name="retryPolicy">The policy that classifies failures and schedules retries.</param>
    /// <param name="observers">The observable that publishes retry lifecycle events.</param>
    public RetryFilter(IRetryPolicy retryPolicy, RetryObservable observers)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateFilterScope("retry");

        _retryPolicy.Probe(scope);
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return RetryPolicyExecution.ExecuteAsync(context, _retryPolicy,
            (policyContext, currentContext) => SendPolicyAsync(context, policyContext, currentContext, next));
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    async Task SendPolicyAsync(TContext context, RetryPolicyContext<TContext> policyContext,
        TContext currentContext, IPipe<TContext> next)
    {
        if (_observers.Count > 0)
            await RetryOperationState.ExecuteAsync(context, () => _observers.PostCreateAsync(policyContext)).ConfigureAwait(false);

        try
        {
            await next.SendAsync(currentContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (RetryPolicyExecution.ShouldPropagate(context, currentContext, exception,
                    () => currentContext.CancellationToken))
                throw;

            if (await PropagateNestedRetryFailureAsync(context, currentContext, exception,
                    token => policyContext.RetryFaultedAsync(exception, token)).ConfigureAwait(false))
                throw;

            if (!RetryPolicyExecution.CanRetry(context, policyContext, exception, out RetryContext<TContext> retryContext))
            {
                if (RetryPolicyExecution.Execute(context, () => _retryPolicy.IsHandled(exception)))
                {
                    RetryOperationState.PublishTerminal(context, retryContext);

                    Task retryFaultedTask = RetryPolicyExecution.ExecuteLifecycleAsync(context, retryContext,
                        token => retryContext.RetryFaultedAsync(exception, token)
                        ?? throw new InvalidOperationException("The retry context returned a null fault task."));
                    if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                        await retryFaultedTask.ConfigureAwait(false);

                    if (_observers.Count > 0)
                    {
                        var retryFaultTask = RetryOperationState.ExecuteAsync(context, () => _observers.RetryFaultAsync(retryContext));
                        if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                            await retryFaultTask.ConfigureAwait(false);
                    }
                }

                throw;
            }

            if (_observers.Count > 0)
            {
                var postFaultTask = RetryOperationState.ExecuteAsync(context, () => _observers.PostFaultAsync(retryContext));
                if (postFaultTask.Status != TaskStatus.RanToCompletion)
                    await postFaultTask.ConfigureAwait(false);
            }

            await AttemptAsync(context, retryContext, next).ConfigureAwait(false);
        }
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    async Task AttemptAsync(TContext context, RetryContext<TContext> retryContext, IPipe<TContext> next)
    {
        while (true)
        {
            TContext currentContext = RetryPolicyExecution.Execute(context, () => retryContext.Context
                ?? throw new InvalidOperationException("The retry policy returned a retry context without a pipe context."));
            using IDisposable currentLifecycle = RetryOperationState.Enter(currentContext);
            await RetryPolicyExecution.ExecuteLifecycleAsync(context, retryContext,
                token => PrepareRetryAsync(context, retryContext, token)).ConfigureAwait(false);

            try
            {
                await next.SendAsync(currentContext).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (RetryPolicyExecution.ShouldPropagate(context, currentContext, exception,
                        () => retryContext.CancellationToken))
                    throw;

                if (await PropagateNestedRetryFailureAsync(context, currentContext, exception,
                        token => retryContext.RetryFaultedAsync(exception, token)).ConfigureAwait(false))
                    throw;

                if (!RetryPolicyExecution.CanRetry(context, retryContext, exception, out RetryContext<TContext> nextRetryContext))
                {
                    if (RetryPolicyExecution.Execute(context, () => _retryPolicy.IsHandled(exception)))
                    {
                        RetryOperationState.PublishTerminal(context, nextRetryContext);

                        Task retryFaultedTask = RetryPolicyExecution.ExecuteLifecycleAsync(context, nextRetryContext,
                            token => nextRetryContext.RetryFaultedAsync(exception, token)
                            ?? throw new InvalidOperationException("The retry context returned a null fault task."));
                        if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                            await retryFaultedTask.ConfigureAwait(false);

                        if (_observers.Count > 0)
                        {
                            var retryFaultTask = RetryOperationState.ExecuteAsync(context, () => _observers.RetryFaultAsync(nextRetryContext));
                            if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultTask.ConfigureAwait(false);
                        }
                    }

                    throw;
                }

                if (_observers.Count > 0)
                {
                    var postFaultTask = RetryOperationState.ExecuteAsync(context, () => _observers.PostFaultAsync(nextRetryContext));
                    if (postFaultTask.Status != TaskStatus.RanToCompletion)
                        await postFaultTask.ConfigureAwait(false);
                }

                retryContext = nextRetryContext;
                continue;
            }

            if (_observers.Count > 0)
                await RetryOperationState.ExecuteAsync(context, () => _observers.RetryCompleteAsync(retryContext)).ConfigureAwait(false);

            return;
        }
    }

    async Task PrepareRetryAsync(TContext context, RetryContext<TContext> retryContext, CancellationToken cancellationToken)
    {
        TimeSpan? delay = retryContext.Delay;
        if (delay.HasValue)
            await Task.Delay(delay.Value, context.GetTimeProvider(), cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        Task preRetryContextTask = retryContext.PreRetryAsync(cancellationToken)
            ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
        await preRetryContextTask.ConfigureAwait(false);

        if (_observers.Count > 0)
            await _observers.PreRetryAsync(retryContext).ConfigureAwait(false);
    }

    async Task<bool> PropagateNestedRetryFailureAsync(TContext rootContext, PipeContext currentContext,
        Exception exception, Func<CancellationToken, Task> notifyPolicyFault)
    {
        // Active terminal ownership propagates the downstream decision without starting another
        // retry budget. Retained diagnostics do not govern a later operation on the same context.
        RetryContext? nestedRetryContext = RetryPolicyExecution.Execute(rootContext,
            () => RetryOperationState.TryGetTerminal(currentContext, exception, out RetryContext? terminal) ? terminal : null);
        if (nestedRetryContext == null)
            return false;

        if (!RetryPolicyExecution.Execute(rootContext, () => _retryPolicy.IsHandled(exception)))
            return true;

        RetryOperationState.PublishTerminal(rootContext, nestedRetryContext);

        Task policyFaultTask = RetryPolicyExecution.ExecuteLifecycleAsync(rootContext, nestedRetryContext,
            token => notifyPolicyFault(token)
            ?? throw new InvalidOperationException("The retry policy returned a null fault task."));
        if (policyFaultTask.Status != TaskStatus.RanToCompletion)
            await policyFaultTask.ConfigureAwait(false);

        if (_observers.Count > 0)
        {
            Task observerTask = RetryOperationState.ExecuteAsync(rootContext, () => _observers.RetryFaultAsync(nestedRetryContext));
            if (observerTask.Status != TaskStatus.RanToCompletion)
                await observerTask.ConfigureAwait(false);
        }

        return true;
    }
}
