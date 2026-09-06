using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for retry saga consume operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class RetrySagaConsumeContext<TSaga> :
    RetryConsumeContext,
    SagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RetrySagaConsumeContext(SagaConsumeContext<TSaga> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>Gets the saga.</summary>
    public TSaga Saga => _context.Saga;

    Task SagaConsumeContext<TSaga>.SetCompletedAsync(CancellationToken cancellationToken)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Gets a value indicating whether completed.</summary>
    public bool IsCompleted => _context.IsCompleted;

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetrySagaConsumeContext<TSaga>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<TContext>.ShortName}");
    }
}
