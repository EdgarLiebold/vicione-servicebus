using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Carries state for rescue exception receive operations.</summary>
public class RescueExceptionReceiveContext :
    ReceiveContextProxy,
    ExceptionReceiveContext
{
    readonly DictionarySendHeaders _headers;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionReceiveContext(ReceiveContext context, Exception exception)
        : base(context)
    {
        Exception = exception;
        ExceptionTimestamp = context.GetTimeProvider().GetUtcNow().UtcDateTime;

        _headers = new DictionarySendHeaders();

        _headers.SetExceptionHeaders(this);
    }

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }
    /// <summary>Gets the exception timestamp.</summary>
    public DateTimeOffset ExceptionTimestamp { get; }

    /// <summary>Gets the exception info.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }

    /// <summary>Gets the exception headers.</summary>
    public SendHeaders ExceptionHeaders => _headers;
}
