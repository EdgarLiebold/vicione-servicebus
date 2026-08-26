namespace ViciOne.ServiceBus.Middleware
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Observables;


    /// <summary>
    /// Uses a retry policy to handle exceptions, retrying the operation in according
    /// with the policy
    /// </summary>
    public class RetryFilter<TContext> :
        IFilter<TContext>
        where TContext : class, PipeContext
    {
        readonly RetryObservable _observers;
        readonly IRetryPolicy _retryPolicy;

        public RetryFilter(IRetryPolicy retryPolicy, RetryObservable observers)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("retry");

            _retryPolicy.Probe(scope);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        async Task IFilter<TContext>.Send(TContext context, IPipe<TContext> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            RetryPolicyContext<TContext> policyContext = _retryPolicy.CreatePolicyContext(context)
                ?? throw new InvalidOperationException("The retry policy returned a null policy context.");
            if (policyContext.Context == null)
            {
                policyContext.Dispose();
                throw new InvalidOperationException("The retry policy returned a policy context without a pipe context.");
            }

            try
            {
                if (_observers.Count > 0)
                {
                    var postCreateTask = _observers.PostCreate(policyContext);
                    if (postCreateTask.Status != TaskStatus.RanToCompletion)
                        await postCreateTask.ConfigureAwait(false);
                }

                await next.Send(policyContext.Context).ConfigureAwait(false);
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

                if (await PropagateNestedRetryFailure(context, policyContext.Context, exception,
                        () => policyContext.RetryFaulted(exception)).ConfigureAwait(false))
                    throw;

                if (!policyContext.CanRetry(exception, out RetryContext<TContext> retryContext))
                {
                    EnsureRetryContext(retryContext);

                    if (_retryPolicy.IsHandled(exception))
                    {
                        context.GetOrAddPayload(() => retryContext);

                        var retryFaultedTask = retryContext.RetryFaulted(exception);
                        if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                            await retryFaultedTask.ConfigureAwait(false);

                        if (_observers.Count > 0)
                        {
                            var retryFaultTask = _observers.RetryFault(retryContext);
                            if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultTask.ConfigureAwait(false);
                        }
                    }

                    throw;
                }

                EnsureRetryContext(retryContext);

                if (_observers.Count > 0)
                {
                    var postFaultTask = _observers.PostFault(retryContext);
                    if (postFaultTask.Status != TaskStatus.RanToCompletion)
                        await postFaultTask.ConfigureAwait(false);
                }

                await Attempt(context, retryContext, next).ConfigureAwait(false);
            }
            finally
            {
                policyContext.Dispose();
            }
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        async Task Attempt(TContext context, RetryContext<TContext> retryContext, IPipe<TContext> next)
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

                var preRetryContextTask = retryContext.PreRetry();
                if (preRetryContextTask.Status != TaskStatus.RanToCompletion)
                    await preRetryContextTask.ConfigureAwait(false);

                if (_observers.Count > 0)
                {
                    var preRetryTask = _observers.PreRetry(retryContext);
                    if (preRetryTask.Status != TaskStatus.RanToCompletion)
                        await preRetryTask.ConfigureAwait(false);
                }

                try
                {
                    await next.Send(retryContext.Context).ConfigureAwait(false);

                    if (_observers.Count > 0)
                    {
                        var retryCompleteTask = _observers.RetryComplete(retryContext);
                        if (retryCompleteTask.Status != TaskStatus.RanToCompletion)
                            await retryCompleteTask.ConfigureAwait(false);
                    }

                    return;
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

                    if (await PropagateNestedRetryFailure(context, retryContext.Context, exception,
                            () => retryContext.RetryFaulted(exception)).ConfigureAwait(false))
                        throw;

                    if (!retryContext.CanRetry(exception, out RetryContext<TContext> nextRetryContext))
                    {
                        EnsureRetryContext(nextRetryContext);

                        if (_retryPolicy.IsHandled(exception))
                        {
                            context.GetOrAddPayload(() => nextRetryContext);

                            var retryFaultedTask = nextRetryContext.RetryFaulted(exception);
                            if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultedTask.ConfigureAwait(false);

                            if (_observers.Count > 0)
                            {
                                var retryFaultTask = _observers.RetryFault(nextRetryContext);
                                if (retryFaultTask.Status != TaskStatus.RanToCompletion)
                                    await retryFaultTask.ConfigureAwait(false);
                            }
                        }

                        throw;
                    }

                    EnsureRetryContext(nextRetryContext);

                    if (_observers.Count > 0)
                    {
                        var postFaultTask = _observers.PostFault(nextRetryContext);
                        if (postFaultTask.Status != TaskStatus.RanToCompletion)
                            await postFaultTask.ConfigureAwait(false);
                    }

                    retryContext = nextRetryContext;
                }
            }
        }

        async Task<bool> PropagateNestedRetryFailure(TContext rootContext, PipeContext currentContext,
            Exception exception, Func<Task> notifyPolicyFault)
        {
            // A downstream retry owns the exception once its context is present. The outer retry
            // reports the terminal fault but must not start a second retry budget. The non-generic
            // payload is deliberate: dispatch may change the concrete PipeContext type, while
            // RetryContext.ContextType retains the exact type required by observer callbacks.
            if (!currentContext.TryGetPayload(out RetryContext nestedRetryContext))
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
                Task observerTask = _observers.RetryFault(nestedRetryContext);
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
}
