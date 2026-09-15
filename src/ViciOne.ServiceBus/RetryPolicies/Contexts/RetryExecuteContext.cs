using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Tracks retry state while preserving execute results between attempts.</summary>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
internal sealed class RetryExecuteContext<TArguments> :
    ExecuteContextScope<TArguments>,
    ConsumeRetryContext
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;
    readonly ExecutionResult? _existingResult;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates an execute retry attempt.</summary>
    /// <param name="context">The execute context being retried.</param>
    /// <param name="retryPolicy">The policy that creates subsequent attempts.</param>
    /// <param name="retryContext">The previous retry attempt, or <see langword="null" /> for the first attempt.</param>
    public RetryExecuteContext(ExecuteContext<TArguments> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _context = context;

        if (retryContext is RetryContext<ExecuteContext<TArguments>> executeRetryContext)
            _existingResult = executeRetryContext.Context.Result;

        Result = new PendingExecutionResult();

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

    /// <summary>Gets the one-based retry attempt represented by this context.</summary>
    public int RetryAttempt { get; }

    /// <summary>Gets the number of retry attempts completed before this context.</summary>
    public int RetryCount { get; }

    /// <summary>Creates the next execute retry context and restores any pre-retry result.</summary>
    /// <typeparam name="TContext">The requested consume-retry context type.</typeparam>
    /// <param name="retryContext">The retry state for the next attempt.</param>
    /// <returns>The next typed retry context.</returns>
    public TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        ArgumentNullException.ThrowIfNull(retryContext);

        if (retryContext is RetryContext<ExecuteContext<TArguments>> executeRetryContext && _existingResult != null)
            executeRetryContext.Context.Result = _existingResult;

        return new RetryExecuteContext<TArguments>(_context, _retryPolicy, retryContext) as TContext
            ?? throw new InvalidOperationException($"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.");
    }

    /// <summary>Restores the original execute result after all retry attempts are exhausted.</summary>
    /// <param name="cancellationToken">Cancels result restoration.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (_existingResult != null && Result is PendingExecutionResult)
            Result = _existingResult;

        return Task.CompletedTask;
    }

    sealed class PendingExecutionResult :
        ExecutionResult
    {
        public Task EvaluateAsync(CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

        public bool IsFaulted([NotNullWhen(true)] out Exception? exception)
        {
            exception = null;
            return false;
        }
    }
}
