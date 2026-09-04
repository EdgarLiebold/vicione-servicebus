using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a base retry policy context implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public abstract class BaseRetryPolicyContext<TContext> :
    RetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly IRetryPolicy _policy;
    readonly Lazy<CancellationTokenSource> _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <param name="context">The operation context.</param>
    protected BaseRetryPolicyContext(IRetryPolicy policy, TContext context)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationTokenSource = new Lazy<CancellationTokenSource>(CreateCancellationTokenSource,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    protected CancellationToken CancellationToken => _cancellationTokenSource.Value.Token;

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public TContext Context { get; }

    /// <summary>
    /// Determines whether the current value can retry.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = CreateRetryContext(exception, CancellationToken);

        return _policy.IsHandled(exception) && !_cancellationTokenSource.Value.IsCancellationRequested;
    }

    Task RetryPolicyContext<TContext>.RetryFaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Value.Cancel();
    }

    void IDisposable.Dispose()
    {
        if (_cancellationTokenSource.IsValueCreated)
            _cancellationTokenSource.Value.Dispose();
    }

    /// <summary>
    /// Creates retry context.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken);

    CancellationTokenSource CreateCancellationTokenSource()
    {
        return Context.CancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken)
            : new CancellationTokenSource();
    }
}
