using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Combines retry-policy state with consume-specific pending-fault handling.</summary>
internal sealed class ConsumeContextRetryContext :
    RetryContext<ConsumeContext>
{
    readonly RetryConsumeContext _context;
    readonly RetryContext<ConsumeContext> _retryContext;

    /// <summary>Initializes an adapter over retry-policy and consume-retry state.</summary>
    /// <param name="retryContext">The retry-policy state.</param>
    /// <param name="context">The consume-retry context that owns pending faults.</param>
    public ConsumeContextRetryContext(RetryContext<ConsumeContext> retryContext, RetryConsumeContext context)
    {
        _retryContext = retryContext ?? throw new ArgumentNullException(nameof(retryContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Gets the token that cancels retry processing.</summary>
    public CancellationToken CancellationToken => _retryContext.CancellationToken;

    /// <summary>Gets the consume context exposed to the retry filter.</summary>
    public ConsumeContext Context => _context;

    /// <summary>Gets the exception that triggered retry evaluation.</summary>
    public Exception Exception => _retryContext.Exception;

    /// <summary>Gets the number of retry attempts completed before this decision.</summary>
    public int RetryCount => _retryContext.RetryCount;

    /// <summary>Gets the one-based retry attempt that follows the failed operation.</summary>
    public int RetryAttempt => _retryContext.RetryAttempt;

    /// <summary>Gets the pipeline context type governed by the policy.</summary>
    public Type ContextType => _retryContext.ContextType;

    /// <summary>Gets the scheduled delay, or <see langword="null" /> for an immediate or terminal decision.</summary>
    public TimeSpan? Delay => _retryContext.Delay;

    /// <summary>Runs the policy's pre-retry work.</summary>
    /// <param name="cancellationToken">The token that cancels pre-retry work.</param>
    /// <returns>A task that completes when the retry may begin.</returns>
    public async Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        Task callback = _retryContext.PreRetryAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
        await callback.ConfigureAwait(false);
    }

    /// <summary>Reports a failed retry and then flushes pending consume faults.</summary>
    /// <param name="exception">The exception raised by the retry attempt.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes after policy and pending-fault notification.</returns>
    public async Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Task callback = _retryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry context returned a null fault task.");
        await callback.ConfigureAwait(false);

        await _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Determines whether another attempt is permitted and creates its consume-aware state.</summary>
    /// <param name="exception">The exception raised by the failed attempt.</param>
    /// <param name="retryContext">The consume-aware state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<ConsumeContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var canRetry = _retryContext.CanRetry(exception, out RetryContext<ConsumeContext> policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        retryContext = new ConsumeContextRetryContext(policyRetryContext, canRetry ? _context.CreateNext(policyRetryContext) : _context);

        return canRetry;
    }
}


/// <summary>Combines retry-policy state with a consume-retry context exposed as a filter contract.</summary>
/// <typeparam name="TFilter">The pipeline contract presented to the retry filter.</typeparam>
/// <typeparam name="TContext">The consume-retry context implementation.</typeparam>
internal sealed class ConsumeContextRetryContext<TFilter, TContext> :
    RetryContext<TFilter>
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly TContext _context;
    readonly RetryContext<TFilter> _retryContext;

    /// <summary>Initializes an adapter over retry-policy and consume-retry state.</summary>
    /// <param name="retryContext">The retry-policy state.</param>
    /// <param name="context">The consume-retry context that owns pending faults.</param>
    public ConsumeContextRetryContext(RetryContext<TFilter> retryContext, TContext context)
    {
        _retryContext = retryContext ?? throw new ArgumentNullException(nameof(retryContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Gets the token that cancels retry processing.</summary>
    public CancellationToken CancellationToken => _retryContext.CancellationToken;

    /// <summary>Gets the context exposed to the retry filter.</summary>
    public TFilter Context => _context;

    /// <summary>Gets the exception that triggered retry evaluation.</summary>
    public Exception Exception => _retryContext.Exception;

    /// <summary>Gets the number of retry attempts completed before this decision.</summary>
    public int RetryCount => _retryContext.RetryCount;

    /// <summary>Gets the one-based retry attempt that follows the failed operation.</summary>
    public int RetryAttempt => _retryContext.RetryAttempt;

    /// <summary>Gets the pipeline context type governed by the policy.</summary>
    public Type ContextType => _retryContext.ContextType;

    /// <summary>Gets the scheduled delay, or <see langword="null" /> for an immediate or terminal decision.</summary>
    public TimeSpan? Delay => _retryContext.Delay;

    /// <summary>Runs the policy's pre-retry work.</summary>
    /// <param name="cancellationToken">The token that cancels pre-retry work.</param>
    /// <returns>A task that completes when the retry may begin.</returns>
    public async Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        Task callback = _retryContext.PreRetryAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry context returned a null pre-retry task.");
        await callback.ConfigureAwait(false);
    }

    /// <summary>Reports a failed retry and then flushes pending consume faults.</summary>
    /// <param name="exception">The exception raised by the retry attempt.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes after policy and pending-fault notification.</returns>
    public async Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Task callback = _retryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry context returned a null fault task.");
        await callback.ConfigureAwait(false);

        await _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Determines whether another attempt is permitted and creates its consume-aware state.</summary>
    /// <param name="exception">The exception raised by the failed attempt.</param>
    /// <param name="retryContext">The consume-aware state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<TFilter> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var canRetry = _retryContext.CanRetry(exception, out RetryContext<TFilter> policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        TContext nextContext = canRetry
            ? _context.CreateNext<TContext>(policyRetryContext)
                ?? throw new InvalidOperationException("The consume retry context returned a null next context.")
            : _context;

        retryContext = new ConsumeContextRetryContext<TFilter, TContext>(policyRetryContext, nextContext);

        return canRetry;
    }
}
