using System;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

public abstract class BaseCourierContext :
    ConsumeContextScope<RoutingSlip>,
    CourierContext
{
    readonly Guid _executionId;
    readonly long _startedAt;
    readonly DateTimeOffset _timestamp;
    readonly TimeProvider _timeProvider;

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

        // ReSharper disable once VirtualMemberCallInConstructor
        Publisher = new RoutingSlipEventPublisher(this, RoutingSlip, CancellationToken);
    }

    protected IRoutingSlipEventPublisher Publisher { get; }
    protected SanitizedRoutingSlip RoutingSlip { get; }

    DateTimeOffset CourierContext.Timestamp => _timestamp;
    TimeSpan CourierContext.Elapsed => _timeProvider.GetElapsedTime(_startedAt);
    Guid CourierContext.TrackingNumber => RoutingSlip.TrackingNumber;
    Guid CourierContext.ExecutionId => _executionId;

    RoutingSlip ConsumeContext<RoutingSlip>.Message => RoutingSlip;

    public abstract string ActivityName { get; }
}
