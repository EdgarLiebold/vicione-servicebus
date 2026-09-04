using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a retry saga consume context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class RetrySagaConsumeContext<TSaga> :
    RetryConsumeContext,
    SagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryContext">The retry context value.</param>
    public RetrySagaConsumeContext(SagaConsumeContext<TSaga> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context, retryPolicy, retryContext)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the saga value.
    /// </summary>
    public TSaga Saga => _context.Saga;

    Task SagaConsumeContext<TSaga>.SetCompletedAsync(CancellationToken cancellationToken)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the is completed value.
    /// </summary>
    public bool IsCompleted => _context.IsCompleted;

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetrySagaConsumeContext<TSaga>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<TContext>.ShortName}");
    }
}
