using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>Provides timing, identity, variables, and event publication shared by activity host contexts.</summary>
internal abstract class BaseCourierContext :
    ConsumeContextScope<RoutingSlip>,
    CourierContext
{
    readonly Guid _executionId;
    readonly long _startedAt;
    readonly DateTimeOffset _timestamp;
    readonly TimeProvider _timeProvider;
    readonly IReadOnlyDictionary<string, object> _variables;

    /// <summary>Creates activity state from a received routing slip and its context-scoped clock.</summary>
    /// <param name="consumeContext">The routing-slip consume context to isolate for activity execution.</param>
    protected BaseCourierContext(ConsumeContext<RoutingSlip> consumeContext)
        : base(consumeContext)
    {
        if (consumeContext == null)
            throw new ArgumentNullException(nameof(consumeContext));

        _timeProvider = consumeContext.GetTimeProvider();
        _startedAt = _timeProvider.GetTimestamp();
        _executionId = NewId.NextGuid();
        _timestamp = _timeProvider.GetUtcNow();

        RoutingSlip = new SanitizedRoutingSlip(consumeContext);
        _variables = RoutingSlip.Variables;

        Publisher = new RoutingSlipEventPublisher(this, RoutingSlip);
    }

    /// <summary>Gets the publisher bound to this routing slip and consume context.</summary>
    protected IRoutingSlipEventPublisher Publisher { get; }
    /// <summary>Gets the detached, validated routing-slip state used by this activity.</summary>
    protected SanitizedRoutingSlip RoutingSlip { get; }

    DateTimeOffset ActivityContext.Timestamp => _timestamp;
    TimeSpan ActivityContext.Elapsed => _timeProvider.GetElapsedTime(_startedAt);
    Guid ActivityContext.TrackingNumber => RoutingSlip.TrackingNumber;
    Guid ActivityContext.ExecutionId => _executionId;
    IReadOnlyDictionary<string, object> ActivityContext.Variables => _variables;

    RoutingSlip ConsumeContext<RoutingSlip>.Message => RoutingSlip;

    /// <summary>Gets the logical name of the activity represented by this context.</summary>
    public abstract string ActivityName { get; }

    Task ActivityContext.NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken)
    {
        return NotifyConsumedAsync((ConsumeContext<RoutingSlip>)this, duration, consumerType, cancellationToken);
    }
}
