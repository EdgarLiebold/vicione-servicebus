using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Projects a failed receive context and its exception into a rescue pipeline.</summary>
internal sealed class RescueExceptionReceiveContext :
    ReceiveContextProxy,
    ExceptionReceiveContext
{
    readonly DictionarySendHeaders _headers;
    ExceptionInfo? _exceptionInfo;

    /// <summary>Creates a rescue projection for a failed receive operation.</summary>
    /// <param name="context">The failed receive context.</param>
    /// <param name="exception">The failure exposed to the rescue pipeline.</param>
    public RescueExceptionReceiveContext(ReceiveContext context, Exception exception)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        ExceptionTimestamp = context.GetTimeProvider().GetUtcNow();

        _headers = new DictionarySendHeaders();

        _headers.SetExceptionHeaders(this);
    }

    /// <summary>Gets the failure exposed to the rescue pipeline.</summary>
    public Exception Exception { get; }
    /// <summary>Gets the timestamp captured when the rescue projection was created.</summary>
    public DateTimeOffset ExceptionTimestamp { get; }

    /// <summary>Gets the structured snapshot of the failure.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }

    /// <summary>Gets the transport headers derived from the failure.</summary>
    public SendHeaders ExceptionHeaders => _headers;
}
