using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a courier context proxy implementation.
/// </summary>
public abstract class CourierContextProxy :
    ConsumeContextProxy<RoutingSlip>,
    CourierContext
{
    readonly CourierContext _courierContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="courierContext">The courier context value.</param>
    protected CourierContextProxy(CourierContext courierContext)
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
