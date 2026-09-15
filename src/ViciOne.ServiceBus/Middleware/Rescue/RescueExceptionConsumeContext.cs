using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events.Faults;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Projects a failed typed consume context and its exception into a rescue pipeline.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
internal sealed class RescueExceptionConsumeContext<TMessage> :
    ConsumeContextProxy<TMessage>,
    ExceptionConsumeContext<TMessage>
    where TMessage : class
{
    ExceptionInfo? _exceptionInfo;

    /// <summary>Creates a typed rescue projection for a failed consume operation.</summary>
    /// <param name="context">The failed consume context.</param>
    /// <param name="exception">The failure exposed to the rescue pipeline.</param>
    public RescueExceptionConsumeContext(ConsumeContext<TMessage> context, Exception exception)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>Gets the failure exposed to the rescue pipeline.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the structured snapshot of the failure.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}


/// <summary>Projects a failed untyped consume context and its exception into a rescue pipeline.</summary>
internal sealed class RescueExceptionConsumeContext :
    ConsumeContextProxy,
    ExceptionConsumeContext
{
    ExceptionInfo? _exceptionInfo;

    /// <summary>Creates an untyped rescue projection for a failed consume operation.</summary>
    /// <param name="context">The failed consume context.</param>
    /// <param name="exception">The failure exposed to the rescue pipeline.</param>
    public RescueExceptionConsumeContext(ConsumeContext context, Exception exception)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>Gets the failure exposed to the rescue pipeline.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the structured snapshot of the failure.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
