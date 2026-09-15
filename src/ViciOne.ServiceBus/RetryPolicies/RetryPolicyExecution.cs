using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Owns policy admission, classification and cleanup within a retry operation.</summary>
internal static class RetryPolicyExecution
{
    /// <summary>Runs a policy-governed operation and releases its acquired context exactly once.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="context">The input context whose operation owns infrastructure failures.</param>
    /// <param name="policy">The policy that creates and classifies retry state.</param>
    /// <param name="send">The processing operation and its validated initial context.</param>
    /// <returns>
    /// A task that preserves an escaped failure. If processing and cleanup both fail, an
    /// <see cref="AggregateException" /> preserves the primary failure followed by the cleanup failure.
    /// </returns>
    public static async Task ExecuteAsync<TContext>(TContext context, IRetryPolicy policy,
        Func<RetryPolicyContext<TContext>, TContext, Task> send)
        where TContext : class, PipeContext
    {
        using IDisposable root = RetryOperationState.BeginPolicy(context);
        try
        {
            RetryPolicyContext<TContext>? policyContext = null;
            Exception? primaryFailure = null;
            try
            {
                policyContext = Execute(context, () => policy.CreatePolicyContext(context)
                    ?? throw new InvalidOperationException("The retry policy returned a null policy context."));
                TContext currentContext = Execute(context, () => policyContext.Context
                    ?? throw new InvalidOperationException("The retry policy returned a policy context without a pipe context."));
                using IDisposable current = Execute(context, () => RetryOperationState.Enter(currentContext));
                await send(policyContext, currentContext).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
                throw;
            }
            finally
            {
                if (policyContext != null)
                {
                    try
                    {
                        policyContext.Dispose();
                    }
                    catch (Exception cleanupFailure)
                    {
                        if (primaryFailure == null)
                        {
                            RetryOperationState.Mark(context, cleanupFailure);
                            throw;
                        }

                        var combinedFailure = new AggregateException("Retry processing and policy cleanup both failed.",
                            primaryFailure, cleanupFailure);
                        RetryOperationState.Mark(context, combinedFailure);
                        throw combinedFailure;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            RetryOperationState.Propagate(context, exception);
            throw;
        }
    }

    /// <summary>Releases acquired policy state after failed admission without discarding a cleanup failure.</summary>
    /// <param name="policyContext">The acquired wrapper or underlying policy state whose admission failed.</param>
    /// <param name="primaryFailure">The exact admission, projection or representation failure.</param>
    /// <exception cref="AggregateException">Admission and cleanup both failed, in that order.</exception>
    public static void DisposeAfterFactoryFailure(IDisposable policyContext, Exception primaryFailure)
    {
        try
        {
            policyContext.Dispose();
        }
        catch (Exception cleanupFailure)
        {
            throw new AggregateException("Retry policy admission and cleanup both failed.", primaryFailure, cleanupFailure);
        }
    }

    /// <summary>Protects lifecycle failures and requested cancellation before evaluating a business retry.</summary>
    /// <param name="context">The input operation that owns infrastructure failures and source cancellation.</param>
    /// <param name="currentContext">The downstream context that carries escaped lifecycle ownership.</param>
    /// <param name="exception">The failure raised by the downstream operation.</param>
    /// <param name="getRetryToken">Reads the applicable policy or current-context cancellation token.</param>
    /// <returns>Whether the original failure must propagate rather than consume a business retry budget.</returns>
    public static bool ShouldPropagate(PipeContext context, PipeContext currentContext, Exception exception,
        Func<CancellationToken> getRetryToken)
    {
        if (Execute(context, () => RetryOperationState.IsOwned(currentContext, exception)))
        {
            RetryOperationState.Mark(context, exception);
            return true;
        }

        CancellationToken sourceToken = Execute(context, () => context.CancellationToken);
        sourceToken.ThrowIfCancellationRequested();
        if (exception is OperationCanceledException cancellation && cancellation.CancellationToken.IsCancellationRequested)
        {
            CancellationToken retryToken = Execute(context, getRetryToken);
            if (cancellation.CancellationToken == retryToken)
                return true;
        }

        CancellationToken currentToken = Execute(context, () => currentContext.CancellationToken);
        currentToken.ThrowIfCancellationRequested();
        return false;
    }

    /// <summary>Awaits lifecycle work canceled by either the input operation or its selected policy decision.</summary>
    /// <param name="context">The input operation that owns lifecycle failures and source cancellation.</param>
    /// <param name="retryContext">The decision whose token independently cancels policy work.</param>
    /// <param name="execute">The lifecycle work that observes the effective cancellation token.</param>
    /// <returns>A task that preserves failures and identifies requested cancellation by its original token.</returns>
    public static Task ExecuteLifecycleAsync(PipeContext context, RetryContext retryContext,
        Func<CancellationToken, Task> execute) => RetryOperationState.ExecuteAsync(context,
            () => ExecuteCancelableAsync(context, retryContext, execute));

    static async Task ExecuteCancelableAsync(PipeContext context, RetryContext retryContext,
        Func<CancellationToken, Task> execute)
    {
        CancellationToken sourceToken = context.CancellationToken;
        CancellationToken retryToken = retryContext.CancellationToken;
        using CancellationTokenSource? linkedCancellation = sourceToken.CanBeCanceled && retryToken.CanBeCanceled
            && sourceToken != retryToken ? CancellationTokenSource.CreateLinkedTokenSource(sourceToken, retryToken) : null;
        CancellationToken effectiveToken = linkedCancellation?.Token ?? (retryToken.CanBeCanceled ? retryToken : sourceToken);
        try
        {
            sourceToken.ThrowIfCancellationRequested();
            retryToken.ThrowIfCancellationRequested();
            Task work = execute(effectiveToken)
                ?? throw new InvalidOperationException("The retry lifecycle work returned a null task.");
            await work.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (sourceToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(sourceToken);
        }
        catch (OperationCanceledException) when (retryToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(retryToken);
        }
    }

    /// <summary>Evaluates the initial business failure without reclassifying escaped policy infrastructure failures.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="context">The input context that carries infrastructure ownership.</param>
    /// <param name="policyContext">The policy context that makes the initial decision.</param>
    /// <param name="exception">The business failure being evaluated.</param>
    /// <param name="retryContext">The validated retry or terminal decision.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    public static bool CanRetry<TContext>(PipeContext context, RetryPolicyContext<TContext> policyContext,
        Exception exception, out RetryContext<TContext> retryContext)
        where TContext : class, PipeContext
    {
        try
        {
            bool allowed = policyContext.CanRetry(exception, out retryContext);
            EnsureDecision(retryContext);
            return allowed;
        }
        catch (Exception infrastructureFailure)
        {
            RetryOperationState.Mark(context, infrastructureFailure);
            throw;
        }
    }

    /// <summary>Evaluates a subsequent business failure and validates the resulting policy decision.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="context">The input context that carries infrastructure ownership.</param>
    /// <param name="current">The decision whose retry attempt failed.</param>
    /// <param name="exception">The business failure being evaluated.</param>
    /// <param name="retryContext">The validated next or terminal decision.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    public static bool CanRetry<TContext>(PipeContext context, RetryContext<TContext> current,
        Exception exception, out RetryContext<TContext> retryContext)
        where TContext : class, PipeContext
    {
        try
        {
            bool allowed = current.CanRetry(exception, out retryContext);
            EnsureDecision(retryContext);
            return allowed;
        }
        catch (Exception infrastructureFailure)
        {
            RetryOperationState.Mark(context, infrastructureFailure);
            throw;
        }
    }

    /// <summary>Executes synchronous policy infrastructure and preserves its exact escaped failure.</summary>
    /// <typeparam name="T">The infrastructure result type.</typeparam>
    /// <param name="context">The context that carries infrastructure ownership.</param>
    /// <param name="execute">The factory, classifier or admission work to run.</param>
    /// <returns>The successful infrastructure result.</returns>
    public static T Execute<T>(PipeContext context, Func<T> execute)
    {
        try
        {
            return execute();
        }
        catch (Exception exception)
        {
            RetryOperationState.Mark(context, exception);
            throw;
        }
    }

    static void EnsureDecision<TContext>(RetryContext<TContext> retryContext)
        where TContext : class, PipeContext
    {
        if (retryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");
        if (retryContext.Context == null)
            throw new InvalidOperationException("The retry policy returned a retry context without a pipe context.");
    }
}
