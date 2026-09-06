using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Buffers activity side effects while preserving activity execution metadata.</summary>
public abstract class InMemoryOutboxActivityContextProxy :
    InMemoryOutboxConsumeContext,
    ActivityContext
{
    readonly ActivityContext _activityContext;

    /// <summary>Initializes the outbox context for the specified activity context.</summary>
    /// <param name="activityContext">The activity context.</param>
    protected InMemoryOutboxActivityContextProxy(ActivityContext activityContext)
        : base(activityContext)
    {
        _activityContext = activityContext;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp => _activityContext.Timestamp;

    /// <inheritdoc />
    public TimeSpan Elapsed => _activityContext.Elapsed;

    /// <inheritdoc />
    public Guid TrackingNumber => _activityContext.TrackingNumber;

    /// <inheritdoc />
    public Guid ExecutionId => _activityContext.ExecutionId;

    /// <inheritdoc />
    public string ActivityName => _activityContext.ActivityName;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Variables => _activityContext.Variables;

    /// <inheritdoc />
    public Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default) =>
        _activityContext.NotifyActivityConsumedAsync(duration, consumerType, cancellationToken);
}
