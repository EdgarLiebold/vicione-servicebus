namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using Middleware;


    public static class PipeRetryExtensions
    {
        public static Task Retry(this IRetryPolicy retryPolicy, Func<Task> retryMethod, CancellationToken cancellationToken = default)
        {
            return Retry(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
        }

        public static Task Retry(this IRetryPolicy retryPolicy, Func<Task> retryMethod, TimeProvider timeProvider,
            CancellationToken cancellationToken = default)
        {
            return Retry(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
        }

        public static async Task Retry(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, CancellationToken cancellationToken = default)
        {
            await Retry(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
        }

        public static async Task Retry(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, TimeProvider timeProvider,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(retryMethod);

            await Execute(retryPolicy, async () =>
            {
                await retryMethod().ConfigureAwait(false);
                return true;
            }, log, timeProvider, cancellationToken).ConfigureAwait(false);
        }

        public static Task<T> Retry<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, CancellationToken cancellationToken = default)
        {
            return Retry(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
        }

        public static Task<T> Retry<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, TimeProvider timeProvider,
            CancellationToken cancellationToken = default)
        {
            return Retry(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
        }

        public static async Task<T> Retry<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log,
            CancellationToken cancellationToken = default)
        {
            return await Retry(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T> Retry<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log, TimeProvider timeProvider,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(retryMethod);

            return await Execute(retryPolicy, retryMethod, log, timeProvider, cancellationToken).ConfigureAwait(false);
        }

        static async Task<TResult> Execute<TResult>(IRetryPolicy retryPolicy, Func<Task<TResult>> retryMethod, bool log,
            TimeProvider timeProvider, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(retryPolicy);
            ArgumentNullException.ThrowIfNull(timeProvider);
            cancellationToken.ThrowIfCancellationRequested();

            var context = new InlinePipeContext(cancellationToken);
            context.SetTimeProvider(timeProvider);

            using RetryPolicyContext<InlinePipeContext> policyContext = retryPolicy.CreatePolicyContext(context)
                ?? throw new InvalidOperationException("The retry policy returned a null policy context.");
            if (policyContext.Context == null)
                throw new InvalidOperationException("The retry policy returned a policy context without a pipe context.");

            RetryContext<InlinePipeContext> retryContext = null;
            while (true)
            {
                try
                {
                    if (retryContext != null)
                    {
                        retryContext.CancellationToken.ThrowIfCancellationRequested();

                        if (log)
                            LogContext.Warning?.Log(retryContext.Exception, "Retrying {Delay}: {Message}", retryContext.Delay,
                                retryContext.Exception.Message);

                        if (retryContext.Delay.HasValue)
                        {
                            await Task.Delay(retryContext.Delay.Value, timeProvider, retryContext.CancellationToken)
                                .ConfigureAwait(false);
                        }

                        Task preRetryTask = retryContext.PreRetry()
                            ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
                        if (preRetryTask.Status != TaskStatus.RanToCompletion)
                            await preRetryTask.ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return await retryMethod().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    bool canRetry = retryContext == null
                        ? policyContext.CanRetry(exception, out RetryContext<InlinePipeContext> nextRetryContext)
                        : retryContext.CanRetry(exception, out nextRetryContext);
                    EnsureRetryContext(nextRetryContext);

                    if (!canRetry)
                    {
                        if (retryPolicy.IsHandled(exception))
                        {
                            Task retryFaultedTask = nextRetryContext.RetryFaulted(exception)
                                ?? throw new InvalidOperationException("The retry context returned a null fault task.");
                            if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                                await retryFaultedTask.ConfigureAwait(false);
                        }

                        throw;
                    }

                    retryContext = nextRetryContext;
                }
            }
        }

        static void EnsureRetryContext(RetryContext<InlinePipeContext> retryContext)
        {
            if (retryContext == null)
                throw new InvalidOperationException("The retry policy returned a null retry context.");
        }


        class InlinePipeContext :
            BasePipeContext
        {
            public InlinePipeContext(CancellationToken cancellationToken)
                : base(cancellationToken)
            {
            }
        }
    }
}
