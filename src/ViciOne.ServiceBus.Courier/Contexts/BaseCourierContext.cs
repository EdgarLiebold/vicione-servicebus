using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for base courier operations.</summary>
internal abstract class BaseCourierContext :
    ConsumeContextScope<RoutingSlip>,
    CourierContext
{
    readonly Guid _executionId;
    readonly long _startedAt;
    readonly DateTimeOffset _timestamp;
    readonly TimeProvider _timeProvider;
    readonly IReadOnlyDictionary<string, object> _variables;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeContext">The consume context.</param>
    protected BaseCourierContext(ConsumeContext<RoutingSlip> consumeContext)
        : base(consumeContext)
    {
        if (consumeContext == null)
            throw new ArgumentNullException(nameof(consumeContext));

        _timeProvider = consumeContext.GetTimeProvider();
        _startedAt = _timeProvider.GetTimestamp();
        var newId = NewId.Next();

        _executionId = newId.ToGuid();
        _timestamp = newId.Timestamp;

        RoutingSlip = new SanitizedRoutingSlip(consumeContext);
        _variables = new ReadOnlyDictionary<string, object>(RoutingSlip.Variables);

        Publisher = new RoutingSlipEventPublisher(this, RoutingSlip);
    }

    /// <summary>Gets the publisher.</summary>
    protected IRoutingSlipEventPublisher Publisher { get; }
    /// <summary>Gets the routing slip.</summary>
    protected SanitizedRoutingSlip RoutingSlip { get; }

    DateTimeOffset ActivityContext.Timestamp => _timestamp;
    TimeSpan ActivityContext.Elapsed => _timeProvider.GetElapsedTime(_startedAt);
    Guid ActivityContext.TrackingNumber => RoutingSlip.TrackingNumber;
    Guid ActivityContext.ExecutionId => _executionId;
    IReadOnlyDictionary<string, object> ActivityContext.Variables => _variables;

    RoutingSlip ConsumeContext<RoutingSlip>.Message => RoutingSlip;

    /// <summary>Gets the activity name.</summary>
    public abstract string ActivityName { get; }

    Task ActivityContext.NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken)
    {
        return NotifyConsumedAsync((ConsumeContext<RoutingSlip>)this, duration, consumerType, cancellationToken);
    }
}
