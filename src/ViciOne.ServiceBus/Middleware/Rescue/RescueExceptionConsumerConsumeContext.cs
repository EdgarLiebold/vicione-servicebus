using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

public class RescueExceptionConsumerConsumeContext<TConsumer> :
    ConsumeContextProxy,
    ExceptionConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;
    ExceptionInfo _exceptionInfo;

    public RescueExceptionConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    public TConsumer Consumer => _context.Consumer;

    public Exception Exception { get; }

    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
