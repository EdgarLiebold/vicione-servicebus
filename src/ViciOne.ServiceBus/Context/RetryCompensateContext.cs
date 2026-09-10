using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Tracks retry state while preserving compensation results between attempts.</summary>
/// <typeparam name="TLog">The compensation log contract.</typeparam>
public class RetryCompensateContext<TLog> :
    CompensateContextScope<TLog>,
    ConsumeRetryContext
    where TLog : class
{
    readonly CompensateContext<TLog> _context;
    readonly CompensationResult? _existingResult;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a compensation retry attempt.</summary>
    /// <param name="context">The compensation context being retried.</param>
    /// <param name="retryPolicy">The policy that creates subsequent attempts.</param>
    /// <param name="retryContext">The previous retry attempt, or <see langword="null" /> for the first attempt.</param>
    public RetryCompensateContext(CompensateContext<TLog> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _context = context;

        if (retryContext is RetryContext<CompensateContext<TLog>> compensateRetryContext)
            _existingResult = compensateRetryContext.Context.Result;

        Result = new RetryCompensationResult();

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
    }

    /// <summary>Gets the retry attempt.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the retry count.</summary>
    public int RetryCount { get; }

    /// <summary>Creates the next compensation retry context and restores any pre-retry result.</summary>
    /// <typeparam name="TContext">The requested consume-retry context type.</typeparam>
    /// <param name="retryContext">The retry state for the next attempt.</param>
    /// <returns>The next typed retry context.</returns>
    public TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        if (retryContext is RetryContext<CompensateContext<TLog>> compensateRetryContext && _existingResult != null)
            compensateRetryContext.Context.Result = _existingResult;

        return new RetryCompensateContext<TLog>(_context, _retryPolicy, retryContext) as TContext
            ?? throw new InvalidOperationException($"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.");
    }

    /// <summary>Restores the original compensation result after all retry attempts are exhausted.</summary>
    /// <param name="cancellationToken">Cancels result restoration.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (_existingResult != null && Result is RetryCompensationResult)
            Result = _existingResult;

        return Task.CompletedTask;
    }

    class RetryCompensationResult :
        CompensationResult
    {
        readonly Exception? _exception;

        public RetryCompensationResult(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task EvaluateAsync(CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

        public bool IsFailed([NotNullWhen(true)] out Exception? exception)
        {
            exception = _exception;
            return exception != null;
        }
    }
}
