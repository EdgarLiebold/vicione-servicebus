using System;
using System.Threading;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Middleware.Timeout;

internal abstract class TimeoutCourierContextProxy :
    TimeoutConsumeContext<RoutingSlip>,
    CourierContext
{
    readonly CourierContext _courierContext;

    protected TimeoutCourierContextProxy(CourierContext courierContext, CancellationToken cancellationToken, TimeSpan timeout)
        : base(courierContext, cancellationToken, timeout)
    {
        _courierContext = courierContext;
    }

    DateTime CourierContext.Timestamp => _courierContext.Timestamp;
    TimeSpan CourierContext.Elapsed => _courierContext.Elapsed;
    Guid CourierContext.TrackingNumber => _courierContext.TrackingNumber;
    Guid CourierContext.ExecutionId => _courierContext.ExecutionId;
    string CourierContext.ActivityName => _courierContext.ActivityName;
}
