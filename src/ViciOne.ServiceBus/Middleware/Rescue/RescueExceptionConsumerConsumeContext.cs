using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Carries state for rescue exception consumer consume operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class RescueExceptionConsumerConsumeContext<TConsumer> :
    ConsumeContextProxy,
    ExceptionConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    /// <summary>Gets the consumer.</summary>
    public TConsumer Consumer => _context.Consumer;

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
