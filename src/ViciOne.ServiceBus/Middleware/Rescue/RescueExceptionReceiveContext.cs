using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>
/// Provides a rescue exception receive context implementation.
/// </summary>
public class RescueExceptionReceiveContext :
    ReceiveContextProxy,
    ExceptionReceiveContext
{
    readonly DictionarySendHeaders _headers;
    ExceptionInfo _exceptionInfo = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public RescueExceptionReceiveContext(ReceiveContext context, Exception exception)
        : base(context)
    {
        Exception = exception;
        ExceptionTimestamp = context.GetTimeProvider().GetUtcNow().UtcDateTime;

        _headers = new DictionarySendHeaders();

        _headers.SetExceptionHeaders(this);
    }

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception Exception { get; }
    /// <summary>
    /// Gets the exception timestamp value.
    /// </summary>
    public DateTimeOffset ExceptionTimestamp { get; }

    /// <summary>
    /// Gets the exception info value.
    /// </summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }

    /// <summary>
    /// Gets the exception headers value.
    /// </summary>
    public SendHeaders ExceptionHeaders => _headers;
}
