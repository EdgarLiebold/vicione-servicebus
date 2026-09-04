using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

public abstract class InMemoryOutboxCourierContextProxy :
    InMemoryOutboxConsumeContext<RoutingSlip>,
    CourierContext
{
    readonly CourierContext _courierContext;

    protected InMemoryOutboxCourierContextProxy(CourierContext courierContext)
        : base(courierContext)
    {
        _courierContext = courierContext;
    }

    DateTimeOffset CourierContext.Timestamp => _courierContext.Timestamp;
    TimeSpan CourierContext.Elapsed => _courierContext.Elapsed;
    Guid CourierContext.TrackingNumber => _courierContext.TrackingNumber;
    Guid CourierContext.ExecutionId => _courierContext.ExecutionId;
    string CourierContext.ActivityName => _courierContext.ActivityName;
}
