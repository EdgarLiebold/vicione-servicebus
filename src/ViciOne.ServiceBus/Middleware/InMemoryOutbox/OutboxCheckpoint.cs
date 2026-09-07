using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Identifies an opaque state boundary within the in-memory outbox that created it.
/// </summary>
public sealed class OutboxCheckpoint
{
    internal OutboxCheckpoint(
        InMemoryOutboxConsumeContext owner,
        int deferredMethodCount,
        InMemoryOutboxMessageSchedulerContext.Checkpoint schedulerCheckpoint,
        IReadOnlyList<OutboxCheckpoint>? childCheckpoints = null)
    {
        Owner = owner;
        DeferredMethodCount = deferredMethodCount;
        SchedulerCheckpoint = schedulerCheckpoint;
        ChildCheckpoints = childCheckpoints ?? Array.Empty<OutboxCheckpoint>();
    }

    internal IReadOnlyList<OutboxCheckpoint> ChildCheckpoints { get; }

    internal int DeferredMethodCount { get; }

    internal InMemoryOutboxConsumeContext Owner { get; }

    internal InMemoryOutboxMessageSchedulerContext.Checkpoint SchedulerCheckpoint { get; }
}
