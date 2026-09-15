using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Coordinates policy state, consume retry state, and bus-lifetime cancellation.</summary>
internal sealed class ConsumeContextRetryPolicyContext :
    RetryPolicyContext<ConsumeContext>
{
    readonly RetryConsumeContext _context;
    readonly RetryPolicyContext<ConsumeContext> _policyContext;
    readonly CancellationTokenRegistration _registration;

    /// <summary>Creates operation-scoped state for a consume retry.</summary>
    /// <param name="policyContext">The underlying policy state.</param>
    /// <param name="context">The consume context that owns pending faults.</param>
    /// <param name="cancellationToken">The bus-lifetime token that cancels retry processing.</param>
    public ConsumeContextRetryPolicyContext(RetryPolicyContext<ConsumeContext> policyContext, RetryConsumeContext context,
        CancellationToken cancellationToken)
    {
        _policyContext = policyContext ?? throw new ArgumentNullException(nameof(policyContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _registration = cancellationToken.Register(static state => ((ConsumeContextRetryPolicyContext)state!).Cancel(), this);
    }

    /// <summary>Cancels pending and subsequent retries.</summary>
    public void Cancel()
    {
        _policyContext.Cancel();
    }

    /// <summary>Gets the consume context exposed to the retry filter.</summary>
    public ConsumeContext Context => _context;

    /// <summary>Evaluates a failure and creates matching consume-aware retry state.</summary>
    /// <param name="exception">The failure to evaluate.</param>
    /// <param name="retryContext">The consume-aware state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<ConsumeContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var canRetry = _policyContext.CanRetry(exception, out var policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        if (canRetry)
            _context.LogRetry(exception);

        retryContext = new ConsumeContextRetryContext(policyRetryContext, canRetry ? _context.CreateNext(policyRetryContext) : _context);

        return canRetry;
    }

    /// <summary>Flushes pending consume faults and notifies the underlying policy.</summary>
    /// <param name="exception">The terminal retry failure.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes when both notification paths finish.</returns>
    public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Task pendingFaultsTask = _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The consume retry context returned a null pending-fault task.");
        Task policyFaultTask = _policyContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry policy returned a null fault task.");

        return Task.WhenAll(pendingFaultsTask, policyFaultTask);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _registration.Dispose();
        _policyContext.Dispose();
    }
}


/// <summary>Coordinates specialized consume state with policy and bus-lifetime cancellation.</summary>
/// <typeparam name="TFilter">The pipeline contract exposed to the retry filter.</typeparam>
/// <typeparam name="TContext">The consume-retry context implementation.</typeparam>
internal sealed class ConsumeContextRetryPolicyContext<TFilter, TContext> :
    RetryPolicyContext<TFilter>
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly TContext _context;
    readonly RetryPolicyContext<TFilter> _policyContext;
    readonly CancellationTokenRegistration _registration;

    /// <summary>Creates operation-scoped state for a projected consume retry.</summary>
    /// <param name="policyContext">The underlying policy state.</param>
    /// <param name="context">The projected consume context that owns pending faults.</param>
    /// <param name="cancellationToken">The bus-lifetime token that cancels retry processing.</param>
    public ConsumeContextRetryPolicyContext(RetryPolicyContext<TFilter> policyContext, TContext context, CancellationToken cancellationToken)
    {
        _policyContext = policyContext ?? throw new ArgumentNullException(nameof(policyContext));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _registration = cancellationToken.Register(static state => ((ConsumeContextRetryPolicyContext<TFilter, TContext>)state!).Cancel(), this);
    }

    /// <summary>Cancels pending and subsequent retries.</summary>
    public void Cancel()
    {
        _policyContext.Cancel();
    }

    /// <summary>Gets the projected context exposed to the retry filter.</summary>
    public TFilter Context => _context;

    /// <summary>Evaluates a failure and creates matching projected retry state.</summary>
    /// <param name="exception">The failure to evaluate.</param>
    /// <param name="retryContext">The projected state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    public bool CanRetry(Exception exception, out RetryContext<TFilter> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var canRetry = _policyContext.CanRetry(exception, out var policyRetryContext);
        if (policyRetryContext == null)
            throw new InvalidOperationException("The retry policy returned a null retry context.");

        if (canRetry && _context is ConsumeContext consumeContext)
            consumeContext.LogRetry(exception);

        TContext nextContext = canRetry
            ? _context.CreateNext<TContext>(policyRetryContext)
                ?? throw new InvalidOperationException("The consume retry context returned a null next context.")
            : _context;
        retryContext = new ConsumeContextRetryContext<TFilter, TContext>(policyRetryContext, nextContext);

        return canRetry;
    }

    /// <summary>Flushes pending consume faults and notifies the underlying policy.</summary>
    /// <param name="exception">The terminal retry failure.</param>
    /// <param name="cancellationToken">The token that cancels fault notification.</param>
    /// <returns>A task that completes when both notification paths finish.</returns>
    public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Task pendingFaultsTask = _context.NotifyPendingFaultsAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The consume retry context returned a null pending-fault task.");
        Task policyFaultTask = _policyContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The retry policy returned a null fault task.");

        return Task.WhenAll(pendingFaultsTask, policyFaultTask);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _registration.Dispose();
        _policyContext.Dispose();
    }
}
