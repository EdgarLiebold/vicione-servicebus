using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events.Faults;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Carries state for rescue exception consume operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RescueExceptionConsumeContext<TMessage> :
    ConsumeContextProxy<TMessage>,
    ExceptionConsumeContext<TMessage>
    where TMessage : class
{
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumeContext(ConsumeContext<TMessage> context, Exception exception)
        : base(context)
    {
        Exception = exception;
    }

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}


/// <summary>Carries state for rescue exception consume operations.</summary>
public class RescueExceptionConsumeContext :
    ConsumeContextProxy,
    ExceptionConsumeContext
{
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionConsumeContext(ConsumeContext context, Exception exception)
        : base(context)
    {
        Exception = exception;
    }

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
