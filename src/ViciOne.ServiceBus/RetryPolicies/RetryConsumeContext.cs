using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Scopes a consume context with retry counters and deferred fault notifications.</summary>
public class RetryConsumeContext :
    ConsumeContextScope,
    ConsumeRetryContext
{
    readonly ConsumeContext _context;
    readonly PendingFaultCollection _pendingFaults;

    /// <summary>Initializes a retry scope over a consume context.</summary>
    /// <param name="context">The consumed message context.</param>
    /// <param name="retryPolicy">The policy that classifies retryable failures.</param>
    /// <param name="retryContext">The active retry state, or <see langword="null" /> before the first retry.</param>
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

    /// <summary>Gets the policy that classifies retryable failures.</summary>
    protected IRetryPolicy RetryPolicy { get; }

    /// <summary>Gets the one-based active retry attempt, or zero before the first retry.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the number of retry attempts completed before the active attempt.</summary>
    public int RetryCount { get; }

    /// <summary>Creates the next typed consume-retry scope.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The policy state for the next attempt.</param>
    /// <returns>The next typed consume-retry context.</returns>
    public virtual TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        throw new InvalidOperationException("This is only supported by a derived type");
    }

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all pending fault observers are notified.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        return _pendingFaults.NotifyAsync(_context, cancellationToken: cancellationToken);
    }

    /// <summary>Defers handled consumer faults until the retry sequence reaches a terminal state.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The typed consume context that faulted.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The consumer exception.</param>
    /// <param name="cancellationToken">The token that cancels immediate notification for an unhandled failure.</param>
    /// <returns>A task that completes after the fault is deferred or forwarded.</returns>
    public override Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType, Exception exception,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (RetryPolicy.IsHandled(exception))
        {
            _pendingFaults.Add(context, duration, consumerType, exception);

            return Task.CompletedTask;
        }

        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Creates the next consume-retry scope.</summary>
    /// <param name="retryContext">The policy state for the next attempt.</param>
    /// <returns>A scope that shares the original consume context and retry policy.</returns>
    public RetryConsumeContext CreateNext(RetryContext retryContext)
    {
        return new RetryConsumeContext(_context, RetryPolicy, retryContext);
    }
}


/// <summary>Scopes a typed consume context with retry counters and deferred fault notifications.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public class RetryConsumeContext<TMessage> :
    RetryConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>Initializes a retry scope over a typed consume context.</summary>
    /// <param name="context">The typed consumed message context.</param>
    /// <param name="retryPolicy">The policy that classifies retryable failures.</param>
    /// <param name="retryContext">The active retry state, or <see langword="null" /> before the first retry.</param>
    public RetryConsumeContext(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context.Advanced(), retryPolicy, retryContext)
    {
        _context = context;
    }

    TMessage ConsumeContext<TMessage>.Message => _context.Message;

    /// <summary>Forwards successful consumer completion to the scoped consume context.</summary>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after successful-consumption notification.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Forwards a consumer fault through the retry scope.</summary>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The consumer exception.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after the fault is deferred or forwarded.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Creates the next typed consume-retry scope.</summary>
    /// <typeparam name="TContext">The requested consume-retry context contract.</typeparam>
    /// <param name="retryContext">The policy state for the next attempt.</param>
    /// <returns>The next typed consume-retry context.</returns>
    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumeContext<TMessage>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<TMessage>.ShortName}");
    }
}
