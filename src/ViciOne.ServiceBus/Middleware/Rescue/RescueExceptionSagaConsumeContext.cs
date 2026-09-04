using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

public class RescueExceptionSagaConsumeContext<TSaga> :
    ConsumeContextProxy,
    ExceptionSagaConsumeContext<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga> _context;
    ExceptionInfo _exceptionInfo = null!;

    public RescueExceptionSagaConsumeContext(SagaConsumeContext<TSaga> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    public TSaga Saga => _context.Saga;

    public Task SetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return _context.SetCompletedAsync(cancellationToken: cancellationToken);
    }

    public bool IsCompleted => _context.IsCompleted;

    public Exception Exception { get; }

    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
