using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Adds retry counters and deferred faults to a saga consume context.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class RetrySagaConsumeContext<TSaga> :
    RetryConsumeContext,
    SagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;

    /// <summary>Creates a retry scope over a saga consume context.</summary>
    /// <param name="context">The saga consume context being retried.</param>
    /// <param name="retryPolicy">The policy that classifies retryable failures.</param>
    /// <param name="retryContext">The active retry state, or <see langword="null" /> before the first retry.</param>
    public RetrySagaConsumeContext(SagaConsumeContext<TSaga> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>Gets the saga instance.</summary>
    public TSaga Saga => _context.Saga;

    Task SagaConsumeContext<TSaga>.SetCompletedAsync(CancellationToken cancellationToken)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Gets whether the saga completed during processing.</summary>
    public bool IsCompleted => _context.IsCompleted;

    /// <summary>Creates the next saga retry scope.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The policy state for the next attempt.</param>
    /// <returns>The next typed consume-retry context.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        return new RetrySagaConsumeContext<TSaga>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException(
                $"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.", nameof(TContext));
    }
}
