using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards routing-slip activity state and consume operations to an underlying Courier context.</summary>
internal abstract class CourierContextProxy :
    ConsumeContextProxy<IRoutingSlip>,
    ICourierContext
{
    readonly ICourierContext _courierContext;

    /// <summary>Creates a forwarding view over an existing Courier context.</summary>
    /// <param name="courierContext">The Courier context whose consume operations and activity state are forwarded.</param>
    protected CourierContextProxy(ICourierContext courierContext)
        : base(courierContext ?? throw new ArgumentNullException(nameof(courierContext)))
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
