using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Middleware.Timeout;

/// <summary>Adds timeout cancellation while preserving an underlying Courier activity context.</summary>
internal abstract class TimeoutCourierContextProxy :
    TimeoutConsumeContext<RoutingSlip>,
    CourierContext
{
    readonly CourierContext _courierContext;

    /// <summary>Creates a timeout-decorated Courier context.</summary>
    /// <param name="courierContext">The Courier context whose activity state is preserved.</param>
    /// <param name="cancellationToken">The token that represents the configured timeout boundary.</param>
    /// <param name="timeout">The positive timeout used for fault diagnostics.</param>
    protected TimeoutCourierContextProxy(CourierContext courierContext, CancellationToken cancellationToken, TimeSpan timeout)
        : base(courierContext ?? throw new ArgumentNullException(nameof(courierContext)), cancellationToken, timeout)
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
