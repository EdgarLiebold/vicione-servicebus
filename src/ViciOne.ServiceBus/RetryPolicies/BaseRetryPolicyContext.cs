using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Owns cancellation and initial decision creation for one retry operation.</summary>
/// <typeparam name="TContext">The governed pipeline context type.</typeparam>
internal abstract class BaseRetryPolicyContext<TContext> :
    RetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly IRetryPolicy _policy;
    readonly Lazy<CancellationTokenSource> _cancellationTokenSource;

    /// <summary>Creates operation-scoped state for a policy and pipeline context.</summary>
    /// <param name="policy">The retry policy.</param>
    /// <param name="context">The pipeline context governed by the policy.</param>
    protected BaseRetryPolicyContext(IRetryPolicy policy, TContext context)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationTokenSource = new Lazy<CancellationTokenSource>(CreateCancellationTokenSource,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the token canceled by the source context or an explicit policy cancellation.</summary>
    protected CancellationToken CancellationToken => _cancellationTokenSource.Value.Token;

    /// <summary>Gets the pipeline context governed by the policy.</summary>
    public TContext Context { get; }

    /// <summary>Evaluates the initial failure and creates the resulting retry state.</summary>
    /// <param name="exception">The exception raised by the initial attempt.</param>
    /// <param name="retryContext">The state for the resulting decision.</param>
    /// <returns><see langword="true" /> when another attempt is permitted; otherwise, <see langword="false" />.</returns>
    public virtual bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var canRetry = _policy.IsHandled(exception) && !_cancellationTokenSource.Value.IsCancellationRequested;
        retryContext = CreateRetryContext(exception, CancellationToken, canRetry);

        return canRetry;
    }

    Task RetryPolicyContext<TContext>.RetryFaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Cancels pending and subsequent retries for this operation.</summary>
    public void Cancel()
    {
        _cancellationTokenSource.Value.Cancel();
    }

    void IDisposable.Dispose()
    {
        if (_cancellationTokenSource.IsValueCreated)
            _cancellationTokenSource.Value.Dispose();
    }

    /// <summary>Creates the initial decision for the concrete timing policy.</summary>
    /// <param name="exception">The exception raised by the initial attempt.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <param name="isRetryScheduled"><see langword="true" /> when the decision schedules another attempt.</param>
    /// <returns>The initial retry state.</returns>
    protected abstract RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken,
        bool isRetryScheduled);

    CancellationTokenSource CreateCancellationTokenSource()
    {
        return Context.CancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken)
            : new CancellationTokenSource();
    }
}
