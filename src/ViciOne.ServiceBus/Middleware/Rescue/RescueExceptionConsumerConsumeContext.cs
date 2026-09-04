using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>
/// Provides a rescue exception consumer consume context implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class RescueExceptionConsumerConsumeContext<TConsumer> :
    ConsumeContextProxy,
    ExceptionConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, Exception exception)
        : base(context)
    {
        _context = context;
        Exception = exception;
    }

    /// <summary>
    /// Gets the consumer value.
    /// </summary>
    public TConsumer Consumer => _context.Consumer;

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
