using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a consume context retry policy context implementation.
/// </summary>
public class ConsumeContextRetryPolicyContext :
    RetryPolicyContext<ConsumeContext>
{
    readonly RetryConsumeContext _context;
    readonly RetryPolicyContext<ConsumeContext> _policyContext;
    readonly CancellationTokenRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policyContext">The policy context value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ConsumeContextRetryPolicyContext(RetryPolicyContext<ConsumeContext> policyContext, RetryConsumeContext context,
        CancellationToken cancellationToken)
    {
        _policyContext = policyContext ?? throw new ArgumentNullException(nameof(policyContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _registration = cancellationToken.Register(static state => ((ConsumeContextRetryPolicyContext)state!).Cancel(), this);
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        _policyContext.Cancel();
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public ConsumeContext Context => _context;

    /// <summary>
    /// Determines whether the current value can retry.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<ConsumeContext> retryContext)
    {
        var canRetry = _policyContext.CanRetry(exception, out var policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        if (canRetry)
            _context.LogRetry(exception);

        retryContext = new ConsumeContextRetryContext(policyRetryContext, canRetry ? _context.CreateNext(policyRetryContext) : _context);

        return canRetry;
    }

    /// <summary>
    /// Performs the retry faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken), _policyContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _registration.Dispose();
        _policyContext.Dispose();
    }
}


/// <summary>
/// Provides a consume context retry policy context implementation.
/// </summary>
/// <typeparam name="TFilter">The t filter type.</typeparam>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ConsumeContextRetryPolicyContext<TFilter, TContext> :
    RetryPolicyContext<TFilter>
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly TContext _context;
    readonly RetryPolicyContext<TFilter> _policyContext;
    readonly CancellationTokenRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policyContext">The policy context value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ConsumeContextRetryPolicyContext(RetryPolicyContext<TFilter> policyContext, TContext context, CancellationToken cancellationToken)
    {
        _policyContext = policyContext ?? throw new ArgumentNullException(nameof(policyContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _registration = cancellationToken.Register(static state => ((ConsumeContextRetryPolicyContext<TFilter, TContext>)state!).Cancel(), this);
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        _policyContext.Cancel();
    }

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public TFilter Context => _context;

    /// <summary>
    /// Determines whether the current value can retry.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<TFilter> retryContext)
    {
        var canRetry = _policyContext.CanRetry(exception, out var policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        if (canRetry && _context is ConsumeContext consumeContext)
            consumeContext.LogRetry(exception);

        retryContext = new ConsumeContextRetryContext<TFilter, TContext>(policyRetryContext,
            canRetry ? _context.CreateNext<TContext>(policyRetryContext) : _context);

        return canRetry;
    }

    /// <summary>
    /// Performs the retry faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken), _policyContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _registration.Dispose();
        _policyContext.Dispose();
    }
}
