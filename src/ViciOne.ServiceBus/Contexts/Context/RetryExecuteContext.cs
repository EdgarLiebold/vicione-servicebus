using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for retry execute operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class RetryExecuteContext<TArguments> :
    ExecuteContextScope<TArguments>,
    ConsumeRetryContext
    where TArguments : class
{
    readonly ExecuteContext<TArguments> _context;
    readonly ExecutionResult _existingResult = null!;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RetryExecuteContext(ExecuteContext<TArguments> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context)
    {
        _retryPolicy = retryPolicy;
        _context = context;

        if (retryContext is RetryContext<ExecuteContext<TArguments>> executeRetryContext)
            _existingResult = executeRetryContext.Context.Result;

        Result = new RetryExecutionResult();

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

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        if (retryContext is RetryContext<ExecuteContext<TArguments>> executeRetryContext && _existingResult != null)
            executeRetryContext.Context.Result = _existingResult;

        return new RetryExecuteContext<TArguments>(_context, _retryPolicy, retryContext) as TContext
            ?? throw new InvalidOperationException($"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.");
    }

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_existingResult != null && Result is RetryExecutionResult)
            Result = _existingResult;

        return Task.CompletedTask;
    }


    class RetryExecutionResult :
        ExecutionResult
    {
        readonly Exception? _exception = null!;

        public RetryExecutionResult(Exception? exception = null)
        {
            _exception = exception;
        }

        public Task EvaluateAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
        }

        public bool IsFaulted([NotNullWhen(true)] out Exception? exception)
        {
            exception = _exception;
            return exception != null;
        }
    }
}
