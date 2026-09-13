using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Adds in-memory-outbox behavior while preserving an underlying Courier activity context.</summary>
internal abstract class InMemoryOutboxCourierContextProxy :
    InMemoryOutboxConsumeContext<IRoutingSlip>,
    ICourierContext
{
    readonly ICourierContext _courierContext;

    /// <summary>Creates an outbox-decorated Courier context.</summary>
    /// <param name="courierContext">The Courier context whose activity state is preserved.</param>
    protected InMemoryOutboxCourierContextProxy(ICourierContext courierContext)
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
