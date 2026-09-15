using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

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
    async Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
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

            if (await PropagateNestedRetryFailureAsync(context, policyContext.Context, exception,
                    () => policyContext.RetryFaultedAsync(exception)).ConfigureAwait(false))
                throw;

            if (!policyContext.CanRetry(exception, out RetryContext<TContext> retryContext))
            {
                EnsureRetryContext(retryContext);

                if (_retryPolicy.IsHandled(exception))
                {
                    context.GetOrAddPayload(() => retryContext);

                    Task retryFaultedTask = retryContext.RetryFaultedAsync(exception)
                        ?? throw new InvalidOperationException("The retry context returned a null fault task.");
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

            EnsureRetryContext(retryContext);

            if (_observers.Count > 0)
            {
                var postFaultTask = _observers.PostFaultAsync(retryContext);
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
            if (context.CancellationToken.IsCancellationRequested)
                context.CancellationToken.ThrowIfCancellationRequested();

            retryContext.CancellationToken.ThrowIfCancellationRequested();

            if (retryContext.Delay.HasValue)
            {
                try
                {
                    await Task.Delay(retryContext.Delay.Value, context.GetTimeProvider(), retryContext.CancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
            }

            CancellationToken retryToken = retryContext.CancellationToken;
            using CancellationTokenSource? linkedCancellation = context.CancellationToken.CanBeCanceled
                && retryToken.CanBeCanceled && context.CancellationToken != retryToken
                    ? CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, retryToken)
                    : null;
            CancellationToken callbackToken = linkedCancellation?.Token
                ?? (retryToken.CanBeCanceled ? retryToken : context.CancellationToken);
            try
            {
                Task preRetryContextTask = retryContext.PreRetryAsync(callbackToken)
                    ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
                await preRetryContextTask.ConfigureAwait(false);

                if (_observers.Count > 0)
                    await _observers.PreRetryAsync(retryContext).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(context.CancellationToken);
            }
            catch (OperationCanceledException) when (retryToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(retryToken);
            }

            try
            {
                await next.SendAsync(retryContext.Context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (OperationCanceledException exception)
                when (exception.CancellationToken.IsCancellationRequested
                    && exception.CancellationToken == retryContext.CancellationToken)
            {
                throw;
            }
            catch (Exception exception)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (await PropagateNestedRetryFailureAsync(context, retryContext.Context, exception,
                        () => retryContext.RetryFaultedAsync(exception)).ConfigureAwait(false))
                    throw;

                if (!retryContext.CanRetry(exception, out RetryContext<TContext> nextRetryContext))
                {
                    EnsureRetryContext(nextRetryContext);

                    if (_retryPolicy.IsHandled(exception))
                    {
                        context.GetOrAddPayload(() => nextRetryContext);

                        Task retryFaultedTask = nextRetryContext.RetryFaultedAsync(exception)
                            ?? throw new InvalidOperationException("The retry context returned a null fault task.");
                        if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                            await retryFaultedTask.ConfigureAwait(false);

                        if (_observers.Count > 0)
                        {
                            var retryFaultTask = _observers.RetryFaultAsync(nextRetryContext);
                            if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultTask.ConfigureAwait(false);
                        }
                    }

                    throw;
                }

                EnsureRetryContext(nextRetryContext);

                if (_observers.Count > 0)
                {
                    var postFaultTask = _observers.PostFaultAsync(nextRetryContext);
                    if (postFaultTask.Status != TaskStatus.RanToCompletion)
                        await postFaultTask.ConfigureAwait(false);
                }

                retryContext = nextRetryContext;
                continue;
            }

            if (_observers.Count > 0)
                await _observers.RetryCompleteAsync(retryContext).ConfigureAwait(false);

            return;
        }
    }

    async Task<bool> PropagateNestedRetryFailureAsync(TContext rootContext, PipeContext currentContext,
        Exception exception, Func<Task> notifyPolicyFault)
    {
        // A downstream retry owns an exception once its context is present. The outer retry
        // propagates that exact terminal context without starting a second retry budget.
        if (!currentContext.TryGetPayload(out RetryContext? nestedRetryContext))
            return false;

        if (!_retryPolicy.IsHandled(exception))
            return true;

        rootContext.GetOrAddPayload(() => nestedRetryContext);

        Task policyFaultTask = notifyPolicyFault()
            ?? throw new InvalidOperationException("The retry policy returned a null fault task.");
        if (policyFaultTask.Status != TaskStatus.RanToCompletion)
            await policyFaultTask.ConfigureAwait(false);

        if (_observers.Count > 0)
        {
            Task observerTask = _observers.RetryFaultAsync(nestedRetryContext);
            if (observerTask.Status != TaskStatus.RanToCompletion)
                await observerTask.ConfigureAwait(false);
        }

        return true;
    }

    static void EnsureRetryContext(RetryContext<TContext> retryContext)
    {
        if (retryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
    }
}
