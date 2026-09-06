using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for consume context retry operations.</summary>
public class ConsumeContextRetryContext :
    RetryContext<ConsumeContext>
{
    readonly RetryConsumeContext _context;
    readonly RetryContext<ConsumeContext> _retryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryContext">The retry context.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ConsumeContextRetryContext(RetryContext<ConsumeContext> retryContext, RetryConsumeContext context)
    {
        _retryContext = retryContext;
        _context = context;
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => _retryContext.CancellationToken;

    /// <summary>Gets the context.</summary>
    public ConsumeContext Context => _context;

    /// <summary>Gets the exception.</summary>
    public Exception Exception => _retryContext.Exception;

    /// <summary>Gets the retry count.</summary>
    public int RetryCount => _retryContext.RetryCount;

    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt => _retryContext.RetryAttempt;

    /// <summary>Gets the context type.</summary>
    public Type ContextType => _retryContext.ContextType;

    /// <summary>Gets the delay.</summary>
    public TimeSpan? Delay => _retryContext.Delay;

    /// <summary>Runs before retry.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        await _retryContext.PreRetryAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reports that retry has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        await _retryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken).ConfigureAwait(false);

        await _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Determines whether the current value can retry.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">Receives the retry context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<ConsumeContext> retryContext)
    {
        var canRetry = _retryContext.CanRetry(exception, out RetryContext<ConsumeContext> policyRetryContext);

        retryContext = new ConsumeContextRetryContext(policyRetryContext, canRetry ? _context.CreateNext(policyRetryContext) : _context);

        return canRetry;
    }
}


/// <summary>Carries state for consume context retry operations.</summary>
/// <typeparam name="TFilter">The filter type.</typeparam>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ConsumeContextRetryContext<TFilter, TContext> :
    RetryContext<TFilter>
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly TContext _context;
    readonly RetryContext<TFilter> _retryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="retryContext">The retry context.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ConsumeContextRetryContext(RetryContext<TFilter> retryContext, TContext context)
    {
        _retryContext = retryContext;
        _context = context;
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => _retryContext.CancellationToken;

    /// <summary>Gets the context.</summary>
    public TFilter Context => _context;

    /// <summary>Gets the exception.</summary>
    public Exception Exception => _retryContext.Exception;

    /// <summary>Gets the retry count.</summary>
    public int RetryCount => _retryContext.RetryCount;

    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt => _retryContext.RetryAttempt;

    /// <summary>Gets the context type.</summary>
    public Type ContextType => _retryContext.ContextType;

    /// <summary>Gets the delay.</summary>
    public TimeSpan? Delay => _retryContext.Delay;

    /// <summary>Runs before retry.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PreRetryAsync(CancellationToken cancellationToken = default)
    {
        await _retryContext.PreRetryAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reports that retry has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        await _retryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken).ConfigureAwait(false);

        await _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Determines whether the current value can retry.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">Receives the retry context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<TFilter> retryContext)
    {
        var canRetry = _retryContext.CanRetry(exception, out RetryContext<TFilter> policyRetryContext);

        retryContext = new ConsumeContextRetryContext<TFilter, TContext>(policyRetryContext,
            canRetry ? _context.CreateNext<TContext>(policyRetryContext) : _context);

        return canRetry;
    }
}
