using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a retry consume context implementation.
/// </summary>
public class RetryConsumeContext :
    ConsumeContextScope,
    ConsumeRetryContext
{
    readonly ConsumeContext _context;
    readonly PendingFaultCollection _pendingFaults;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryContext">The retry context value.</param>
    public RetryConsumeContext(ConsumeContext context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context)
    {
        RetryPolicy = retryPolicy;
        _context = context;

        if (retryContext != null)
        {
            RetryAttempt = retryContext.RetryAttempt;
            RetryCount = retryContext.RetryCount;
        }
        else if (context.TryGetPayload<ConsumeRetryContext>(out var existingRetryContext))
        {
            RetryCount = existingRetryContext.RetryCount;
            RetryAttempt = existingRetryContext.RetryAttempt;
        }

        _pendingFaults = new PendingFaultCollection();
    }

    /// <summary>
    /// Gets the retry policy value.
    /// </summary>
    protected IRetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Gets the retry attempt value.
    /// </summary>
    public int RetryAttempt { get; }

    /// <summary>
    /// Gets the retry count value.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        throw new InvalidOperationException("This is only supported by a derived type");
    }

    /// <summary>
    /// Performs the notify pending faults operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        return _pendingFaults.NotifyAsync(_context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        if (RetryPolicy.IsHandled(exception))
        {
            _pendingFaults.Add(context, duration, consumerType, exception);

            return Task.CompletedTask;
        }

        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public RetryConsumeContext CreateNext(RetryContext retryContext)
    {
        return new RetryConsumeContext(_context, RetryPolicy, retryContext);
    }
}


/// <summary>
/// Provides a retry consume context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RetryConsumeContext<T> :
    RetryConsumeContext,
    ConsumeContext<T>
    where T : class
{
    readonly ConsumeContext<T> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryContext">The retry context value.</param>
    public RetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context.Advanced(), retryPolicy, retryContext)
    {
        _context = context;
    }

    T ConsumeContext<T>.Message => _context.Message;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates next.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="retryContext">The retry context value.</param>
    /// <returns>The result of the operation.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumeContext<T>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<T>.ShortName}");
    }
}
