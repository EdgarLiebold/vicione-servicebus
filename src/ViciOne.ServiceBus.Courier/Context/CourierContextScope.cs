using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>Adds scoped payloads while preserving the underlying Courier activity state.</summary>
internal abstract class CourierContextScope :
    ConsumeContextScope<IRoutingSlip>,
    ICourierContext
{
    readonly ICourierContext _courierContext;

    /// <summary>Creates an activity context scope initialized with local payloads.</summary>
    /// <param name="courierContext">The Courier context whose activity state is preserved.</param>
    /// <param name="payloads">The payload values visible within the new scope.</param>
    protected CourierContextScope(ICourierContext courierContext, params object[] payloads)
        : base(courierContext ?? throw new ArgumentNullException(nameof(courierContext)), payloads)
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
