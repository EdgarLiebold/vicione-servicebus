using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    DateTimeOffset ActivityContext.Timestamp => _courierContext.Timestamp;
    TimeSpan ActivityContext.Elapsed => _courierContext.Elapsed;
    Guid ActivityContext.TrackingNumber => _courierContext.TrackingNumber;
    Guid ActivityContext.ExecutionId => _courierContext.ExecutionId;
    string ActivityContext.ActivityName => _courierContext.ActivityName;
    IReadOnlyDictionary<string, object> ActivityContext.Variables => _courierContext.Variables;
    Task ActivityContext.NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken) =>
        _courierContext.NotifyActivityConsumedAsync(duration, consumerType, cancellationToken);
}
