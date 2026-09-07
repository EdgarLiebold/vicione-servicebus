using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for retry compensate operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class RetryCompensateContext<TLog> :
    CompensateContextScope<TLog>,
    ConsumeRetryContext
    where TLog : class
{
    readonly CompensateContext<TLog> _context;
    readonly CompensationResult? _existingResult;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryContext">The retry context.</param>
    public RetryCompensateContext(CompensateContext<TLog> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        : base(context)
    {
        _retryPolicy = retryPolicy;
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

    /// <summary>Creates next.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="retryContext">The retry context.</param>
    /// <returns>The created next.</returns>
    public TContext CreateNext<TContext>(RetryContext retryContext)
        where TContext : class, ConsumeRetryContext
    {
        if (retryContext is RetryContext<CompensateContext<TLog>> compensateRetryContext && _existingResult != null)
            compensateRetryContext.Context.Result = _existingResult;

        return new RetryCompensateContext<TLog>(_context, _retryPolicy, retryContext) as TContext
            ?? throw new InvalidOperationException($"The retry context cannot be represented as {TypeCache<TContext>.ShortName}.");
    }

    /// <summary>Notifies registered observers about pending faults.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
