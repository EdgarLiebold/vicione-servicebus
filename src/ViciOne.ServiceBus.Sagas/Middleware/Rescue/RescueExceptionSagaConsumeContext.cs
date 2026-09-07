using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Carries state for rescue exception saga consume operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class RescueExceptionSagaConsumeContext<TSaga> :
    ConsumeContextProxy,
    ExceptionSagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionSagaConsumeContext(SagaConsumeContext<TSaga> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    /// <summary>Gets the saga.</summary>
    public TSaga Saga => _context.Saga;

    /// <summary>Sets completed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Gets a value indicating whether completed.</summary>
    public bool IsCompleted => _context.IsCompleted;

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
