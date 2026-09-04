using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RetryPolicies;

public class RetryConsumeContext :
    ConsumeContextScope,
    ConsumeRetryContext
{
    readonly ConsumeContext _context;
    readonly PendingFaultCollection _pendingFaults;

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

    protected IRetryPolicy RetryPolicy { get; }

    public int RetryAttempt { get; }

    public int RetryCount { get; }

    public virtual TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        throw new InvalidOperationException("This is only supported by a derived type");
    }

    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        return _pendingFaults.NotifyAsync(_context, cancellationToken: cancellationToken);
    }

    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        if (RetryPolicy.IsHandled(exception))
        {
            _pendingFaults.Add(context, duration, consumerType, exception);

            return Task.CompletedTask;
        }

        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    public RetryConsumeContext CreateNext(RetryContext retryContext)
    {
        return new RetryConsumeContext(_context, RetryPolicy, retryContext);
    }
}


public class RetryConsumeContext<T> :
    RetryConsumeContext,
    ConsumeContext<T>
    where T : class
{
    readonly ConsumeContext<T> _context;

    public RetryConsumeContext(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context.Advanced(), retryPolicy, retryContext)
    {
        _context = context;
    }

    T ConsumeContext<T>.Message => _context.Message;

    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(_context, duration, consumerType, cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(_context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    public override TContext CreateNext<TContext>(RetryContext retryContext)
    {
        return new RetryConsumeContext<T>(_context, RetryPolicy, retryContext) as TContext
            ?? throw new ArgumentException($"The context type is not valid: {TypeCache<T>.ShortName}");
    }
}
