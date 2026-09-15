using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Events.Faults;

namespace ViciOne.ServiceBus.Middleware.Rescue;

/// <summary>Projects a failed consumer context and its exception into a rescue pipeline.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
internal sealed class RescueExceptionConsumerConsumeContext<TConsumer> :
    ConsumeContextProxy,
    ExceptionConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    readonly ConsumerConsumeContext<TConsumer> _context;
    ExceptionInfo? _exceptionInfo;

    /// <summary>Creates a rescue projection for a failed consumer operation.</summary>
    /// <param name="context">The failed consumer context.</param>
    /// <param name="exception">The failure exposed to the rescue pipeline.</param>
    public RescueExceptionConsumerConsumeContext(ConsumerConsumeContext<TConsumer> context, Exception exception)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>Gets the consumer instance associated with the failed operation.</summary>
    public TConsumer Consumer => _context.Consumer;

    /// <summary>Gets the failure exposed to the rescue pipeline.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the structured snapshot of the failure.</summary>
    public ExceptionInfo ExceptionInfo
    {
        get { return _exceptionInfo ??= new FaultExceptionInfo(Exception); }
    }
}
