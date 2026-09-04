using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>
/// Provides a rescue exception consume context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RescueExceptionConsumeContext<TMessage> :
    ConsumeContextProxy<TMessage>,
    ExceptionConsumeContext<TMessage>
    where TMessage : class
{
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumeContext(ConsumeContext<TMessage> context, Exception exception)
        : base(context)
    {
        Exception = exception;
    }

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


/// <summary>
/// Provides a rescue exception consume context implementation.
/// </summary>
public class RescueExceptionConsumeContext :
    ConsumeContextProxy,
    ExceptionConsumeContext
{
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumeContext(ConsumeContext context, Exception exception)
        : base(context)
    {
        Exception = exception;
    }

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
