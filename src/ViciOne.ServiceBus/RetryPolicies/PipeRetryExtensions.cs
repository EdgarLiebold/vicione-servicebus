using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Executes asynchronous operations under an <see cref="IRetryPolicy" />.</summary>
public static class PipeRetryExtensions
{
    /// <summary>Executes an operation and retries handled failures according to the policy.</summary>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task that completes when the operation succeeds or retry processing terminates.</returns>
    public static Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
    }

    /// <summary>Executes an operation and uses the supplied time source for retry delays.</summary>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="timeProvider">The time source used for retry delays.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task that completes when the operation succeeds or retry processing terminates.</returns>
    public static Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
    }

    /// <summary>Executes an operation with optional retry logging.</summary>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="log"><see langword="true" /> to log each scheduled retry; otherwise, <see langword="false" />.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task that completes when the operation succeeds or retry processing terminates.</returns>
    public static async Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, CancellationToken cancellationToken = default)
    {
        await RetryAsync(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes an operation with optional retry logging and a caller-provided time source.</summary>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="log"><see langword="true" /> to log each scheduled retry; otherwise, <see langword="false" />.</param>
    /// <param name="timeProvider">The time source used for retry delays.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task that completes when the operation succeeds or retry processing terminates.</returns>
    public static async Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryMethod);

        await ExecuteAsync(retryPolicy, async () =>
        {
            Task operation = retryMethod()
                ?? throw new InvalidOperationException("The retry operation returned a null task.");
            await operation.ConfigureAwait(false);
            return true;
        }, log, timeProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes a value-producing operation and retries handled failures according to the policy.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task containing the successful operation result.</returns>
    public static Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
    }

    /// <summary>Executes a value-producing operation and uses the supplied time source for retry delays.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="timeProvider">The time source used for retry delays.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task containing the successful operation result.</returns>
    public static Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
    }

    /// <summary>Executes a value-producing operation with optional retry logging.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="log"><see langword="true" /> to log each scheduled retry; otherwise, <see langword="false" />.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task containing the successful operation result.</returns>
    public static async Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log,
        CancellationToken cancellationToken = default)
    {
        return await RetryAsync(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes a value-producing operation with optional retry logging and a caller-provided time source.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="retryPolicy">The policy that classifies failures and schedules subsequent attempts.</param>
    /// <param name="retryMethod">The asynchronous operation to execute.</param>
    /// <param name="log"><see langword="true" /> to log each scheduled retry; otherwise, <see langword="false" />.</param>
    /// <param name="timeProvider">The time source used for retry delays.</param>
    /// <param name="cancellationToken">The token that cancels the operation and pending retries.</param>
    /// <returns>A task containing the successful operation result.</returns>
    public static async Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryMethod);

        return await ExecuteAsync(retryPolicy, retryMethod, log, timeProvider, cancellationToken).ConfigureAwait(false);
    }

    static async Task<TResult> ExecuteAsync<TResult>(IRetryPolicy retryPolicy, Func<Task<TResult>> retryMethod, bool log,
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

        RetryContext<InlinePipeContext>? retryContext = null;
        while (true)
        {
            if (retryContext != null)
                await PrepareRetryAsync(retryContext, log, timeProvider, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            retryContext?.CancellationToken.ThrowIfCancellationRequested();
            try
            {
                Task<TResult> operation = retryMethod()
                    ?? throw new InvalidOperationException("The retry operation returned a null task.");
                return await operation.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
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
                        Task retryFaultedTask = nextRetryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
                            ?? throw new InvalidOperationException("The retry context returned a null fault task.");
                        await retryFaultedTask.ConfigureAwait(false);
                    }

                    throw;
                }

                retryContext = nextRetryContext;
            }
        }
    }

    static async Task PrepareRetryAsync(RetryContext<InlinePipeContext> retryContext, bool log,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        CancellationToken retryToken = retryContext.CancellationToken;
        using CancellationTokenSource? linkedCancellation = cancellationToken.CanBeCanceled && retryToken.CanBeCanceled
            && cancellationToken != retryToken
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retryToken)
            : null;
        CancellationToken preparationToken = linkedCancellation?.Token
            ?? (retryToken.CanBeCanceled ? retryToken : cancellationToken);

        try
        {
            preparationToken.ThrowIfCancellationRequested();

            if (log)
                LogContext.Warning?.Log(retryContext.Exception, "Retrying {Delay}: {Message}", retryContext.Delay,
                    retryContext.Exception.Message);

            if (retryContext.Delay.HasValue)
            {
                await Task.Delay(retryContext.Delay.Value, timeProvider, preparationToken)
                    .ConfigureAwait(false);
            }

            Task preRetryTask = retryContext.PreRetryAsync(cancellationToken: preparationToken)
                ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
            await preRetryTask.ConfigureAwait(false);
            preparationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException) when (retryToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(retryToken);
        }
    }

    static void EnsureRetryContext(RetryContext<InlinePipeContext> retryContext)
    {
        if (retryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
    }


    sealed class InlinePipeContext :
        BasePipeContext
    {
        public InlinePipeContext(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }
}
