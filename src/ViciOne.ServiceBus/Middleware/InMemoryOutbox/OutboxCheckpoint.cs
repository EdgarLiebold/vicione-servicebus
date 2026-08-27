#nullable enable
namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

using System;
using System.Collections.Generic;


/// <summary>
/// An opaque boundary in an in-memory outbox. The outbox that created the checkpoint is the only
/// component that can interpret it.
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
