using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards courier context operations to an underlying context.</summary>
public abstract class CourierContextProxy :
    ConsumeContextProxy<RoutingSlip>,
    CourierContext
{
    readonly CourierContext _courierContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="courierContext">The courier context.</param>
    protected CourierContextProxy(CourierContext courierContext)
        : base(courierContext)
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
