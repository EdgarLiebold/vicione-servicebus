---
type: guide
title: "Concurrency, partitioning and batching"
description: "Explain prefetch versus execution, local partition ordering, batching and capacity planning."
tags: [servicebus, messaging]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-dc8959237664bfd4bd444a67
    resource: repo://docs/api/batch-time-limits.md
  - id: openwiki-source-ddd01750e56f04c43f7abaac
    resource: repo://src/ViciOne.ServiceBus/Advanced/Registration/BatchConsumerExtensions.cs
  - id: openwiki-source-ae67ae254a0bbf79f775918f
    resource: repo://src/ViciOne.ServiceBus/Batching/Runtime/BatchConsumer.cs
  - id: openwiki-source-fea5e4dc2186dbea511d0408
    resource: repo://src/ViciOne.ServiceBus/Middleware/Partitioning/PartitionCoordinator.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Concurrency, partitioning and batching

Capacity controls exist at several stages. Carrier prefetch/admission controls deliveries entering the receiver; consumer concurrency controls executing work; partitioning serializes related operations locally; batching combines deliveries before invoking a batch consumer. These are complementary, not synonyms.

## Bound the bottleneck

Suppose a consumer writes to a database with a small connection pool. Increasing carrier prefetch may only move the backlog from the broker into application memory. Increasing consumer concurrency can saturate the database and cause retries. A rate limit changes arrival pace but does not cap every in-flight dependency.

Use measured processing time and downstream capacity. Track active operations, latency, retry pressure and oldest pending age. A throughput target alone does not justify unbounded admission.

## Endpoint versus consumer settings

Prefetch and transport QoS affect an endpoint's deliveries. Consumer execution policy affects a component's pipeline. Several consumers sharing one endpoint also share its carrier admission behavior.

The topology validator rejects conflicting endpoint settings and consumer-owned transport QoS on multi-consumer shared endpoints. Configure the endpoint-wide setting at the endpoint owner, rather than letting one definition pretend it controls a private queue.

## Partitioned processing

A partition coordinator hashes keys into a fixed set of independent serialized lanes. Same-key operations select the same lane; different keys can share a lane through collisions. Multiple lanes permit concurrent work.

The repository journey configures a consumer by customer ID:

```csharp
bus.AddConsumer<SubmitOrderConsumer>(consumer =>
    consumer.UsePartitionedConcurrency<SubmitOrder, Guid>(
        partitionCount: 16,
        static message => message.CustomerId));
```

This excerpt requires the typed contract, consumer and normal bus/endpoint configuration. The relevant provider configuration extensions are imported by the journey.

Serialization is local to that coordinator. Another process has its own lanes. Local key serialization therefore does not establish global ordering across replicas, carrier redelivery or every upstream producer.

If you need a database invariant, enforce it in the database or appropriate process-state repository as well. Partitioning reduces contention; it does not replace correctness constraints.

## Batching

A batch consumer receives `IMessageBatch<T>`. The runtime collects individual messages until size/time/terminal conditions choose a batch outcome, then invokes a completed-batch pipeline.

Each individual message pipeline can remain open until the batch's terminal outcome. This matters for carrier locks and prefetch: an endpoint admitting fewer concurrent messages than the configured batch size cannot fill that batch by size before earlier messages finish. It may only progress through its time limit.

```mermaid
flowchart LR
    D["Individual deliveries"] --> G["Group and collect"]
    G --> B["Size/time-selected batch"]
    B --> C["Batch consumer"]
    C --> O["Outcome for retained individual pipelines"]
```

Batching is useful for amortizing database/API overhead. It adds waiting time and shared failure considerations. Decide whether grouped messages really share a compatible effect/transaction boundary.

## Timers and time providers

A batch chooses its time provider at creation. Later overrides affect a new batch, not an already open one. The system provider has a timer-range guard; custom providers own their timer capabilities.

The documented system interval maximum is approximately 49.7 days through its whole-millisecond timer conversion. This guard runs at batch creation, not a universal pre-host-start validation boundary. Do not generalize it to arbitrary custom timers.

Batch cancellation, expired timers and failed dispatch require cleanup of individual retained contexts. The runtime tracks successful delivery separately so batch failure does not pretend every member failed identically.

## Ordering and failure

Queue order is not the same as business commit order under concurrent processing. Redelivery can arrive later, and different keys can complete out of arrival order. A retry inside a partition may hold that lane while a delayed retry releases execution and later re-enters.

A batch partially affecting an external service can fail after some effects occurred. Retries need member-level business idempotency or a genuinely atomic batch transaction.

## Practical tuning sequence

Identify the slow dependency and bound concurrent business operations first. Set endpoint admission compatible with that limit and message memory size. Add partitioning where related updates contend. Add batching only when a downstream operation benefits and locks/timeouts remain compatible.

Then observe latency and failure behavior under representative load. Keep store delivery concurrency separate from consumer concurrency: the outgoing worker can accept more messages at the carrier than the receiving application processes at once.

Source: [partition coordinator](../../src/ViciOne.ServiceBus/Middleware/Partitioning/PartitionCoordinator.cs), [batch runtime](../../src/ViciOne.ServiceBus/Batching/Runtime/BatchConsumer.cs), [batch registration](../../src/ViciOne.ServiceBus/Advanced/Registration/BatchConsumerExtensions.cs), [partition journey](../../samples/DeveloperJourneys/Journey10PartitionedConsumer.cs) and [timer boundary](../../docs/api/batch-time-limits.md).
