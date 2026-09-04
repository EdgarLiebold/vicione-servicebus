using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

public abstract class BaseRetryPolicyContext<TContext> :
    RetryPolicyContext<TContext>
    where TContext : class, PipeContext
{
    readonly IRetryPolicy _policy;
    readonly Lazy<CancellationTokenSource> _cancellationTokenSource;

    protected BaseRetryPolicyContext(IRetryPolicy policy, TContext context)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _cancellationTokenSource = new Lazy<CancellationTokenSource>(CreateCancellationTokenSource,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    protected CancellationToken CancellationToken => _cancellationTokenSource.Value.Token;

    public TContext Context { get; }

    public virtual bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
    {
        retryContext = CreateRetryContext(exception, CancellationToken);

        return _policy.IsHandled(exception) && !_cancellationTokenSource.Value.IsCancellationRequested;
    }

    Task RetryPolicyContext<TContext>.RetryFaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Cancel()
    {
        _cancellationTokenSource.Value.Cancel();
    }

    void IDisposable.Dispose()
    {
        if (_cancellationTokenSource.IsValueCreated)
            _cancellationTokenSource.Value.Dispose();
    }

    protected abstract RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken);

    CancellationTokenSource CreateCancellationTokenSource()
    {
        return Context.CancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(Context.CancellationToken)
            : new CancellationTokenSource();
    }
}
