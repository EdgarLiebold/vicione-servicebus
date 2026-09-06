using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Carries state for retry consume operations.</summary>
public class RetryConsumeContext :
    ConsumeContextScope,
    ConsumeRetryContext
{
    readonly ConsumeContext _context;
    readonly PendingFaultCollection _pendingFaults;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
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

    /// <summary>Gets the retry policy.</summary>
    protected IRetryPolicy RetryPolicy { get; }

    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the retry count.</summary>
    public int RetryCount { get; }

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public virtual TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        throw new InvalidOperationException("This is only supported by a derived type");
    }

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        return _pendingFaults.NotifyAsync(_context, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        if (RetryPolicy.IsHandled(exception))
        {
            _pendingFaults.Add(context, duration, consumerType, exception);

            return Task.CompletedTask;
        }

        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Creates next.</summary>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public RetryConsumeContext CreateNext(RetryContext retryContext)
    {
        return new RetryConsumeContext(_context, RetryPolicy, retryContext);
    }
}


/// <summary>Carries state for retry consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class RetryConsumeContext<T> :
    RetryConsumeContext,
    ConsumeContext<T>
    where T : class
{
    readonly ConsumeContext<T> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context.Advanced(), retryPolicy, retryContext)
    {
        _context = context;
    }

    T ConsumeContext<T>.Message => _context.Message;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumeContext<T>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<T>.ShortName}");
    }
}
