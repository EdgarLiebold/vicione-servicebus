using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>
/// Provides a rescue exception saga consume context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class RescueExceptionSagaConsumeContext<TSaga> :
    ConsumeContextProxy,
    ExceptionSagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionSagaConsumeContext(SagaConsumeContext<TSaga> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    /// <summary>
    /// Gets the saga value.
    /// </summary>
    public TSaga Saga => _context.Saga;

    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the is completed value.
    /// </summary>
    public bool IsCompleted => _context.IsCompleted;

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
