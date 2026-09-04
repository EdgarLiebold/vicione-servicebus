using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides extension methods for pipe retry.
/// </summary>
public static class PipeRetryExtensions
{
    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="log">The log value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, CancellationToken cancellationToken = default)
    {
        await RetryAsync(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="log">The log value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task RetryAsync(this IRetryPolicy retryPolicy, Func<Task> retryMethod, bool log, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(retryMethod);

        await ExecuteAsync(retryPolicy, async () =>
        {
            await retryMethod().ConfigureAwait(false);
            return true;
        }, log, timeProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, TimeProvider.System, cancellationToken);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        return RetryAsync(retryPolicy, retryMethod, true, timeProvider, cancellationToken);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="log">The log value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log,
        CancellationToken cancellationToken = default)
    {
        return await RetryAsync(retryPolicy, retryMethod, log, TimeProvider.System, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the retry operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryMethod">The retry method value.</param>
    /// <param name="log">The log value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> RetryAsync<T>(this IRetryPolicy retryPolicy, Func<Task<T>> retryMethod, bool log, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
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

                    Task preRetryTask = retryContext.PreRetryAsync(cancellationToken: cancellationToken)
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
                        Task retryFaultedTask = nextRetryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
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
