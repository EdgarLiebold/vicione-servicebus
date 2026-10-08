---
type: guide
title: "Failures, retries and redelivery"
description: "Separate pipeline retry, broker redelivery, reliable-store retry, faults, errors and cancellation."
tags: [servicebus, reliability]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-d5c22b068d45c5f4b75a7425
    resource: repo://src/ViciOne.ServiceBus/Configuration/ReceivePipeConfiguration.cs
  - id: openwiki-source-e232b32d9c307f7f60f63287
    resource: repo://src/ViciOne.ServiceBus/Middleware/RetryFilter.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Failures, retries and redelivery

A failure belongs to a particular boundary: producer admission, carrier dispatch, receive processing, a business transaction or a long-lived workflow. ServiceBus has several retry mechanisms because those boundaries have different owners. Applying all of them without considering ownership can multiply attempts and hold resources unnecessarily.

## Immediate processing retry

Receive-pipeline retry invokes downstream processing again while retaining the current delivery. A short intermittent database failure may be recoverable this way. The retry filter awaits the downstream task; throwing after an awaited operation is still visible to it.

Retry selection can include handled exception types and exclusions. An ignored exception vetoes a matching handled branch, so a broad handler does not override an explicit exclusion. Configure selection around actual recoverability: malformed input does not improve after repeated execution.

A retry delay keeps this processing operation alive. With sufficient failing traffic, long delays occupy endpoint execution capacity and can affect prefetch and broker locks. For long outages, a deferred mechanism is usually more appropriate.

Middleware placement determines what is retried. A retry outside scope creation can produce a fresh scope for each attempt; a retry inside an existing transaction can repeat work under the same owner. Review [pipelines](../engineering/middleware-and-extensions.md), [scopes](../messaging/consumers-and-scopes.md) and [transactions](transactions-and-outbox.md) together.

## Redelivery

Redelivery returns the message for a later receive attempt instead of keeping the current processing call active for the entire delay. The transport or scheduler carries the deferred work. That changes the resource and failure boundary: another process may receive the next attempt, and a new scope is created.

An endpoint retry and a later redelivery can both run. A policy allowing several local attempts on each of several deliveries can execute the consumer many more times than either number alone suggests. Budget total elapsed time and effects, not only the length of one retry list.

## Reliable-store retry

The reliable delivery worker retries delivery of a retained outgoing intent. Its policy belongs to outgoing carrier work. It does not rerun the producer's business transaction or directly rerun a remote consumer.

A remote consumer's failure can coexist with successful RabbitMQ carrier acceptance. The outgoing worker has met its carrier boundary even though the consumer later retries or sends a fault. InMemory consumer-completion delivery has a different boundary. See [durable delivery](delivery-and-quarantine.md).

## Faults and failed envelopes

After unsuccessful processing, the receive pipeline can generate a fault and move the failed message to an error transport. A fault is a message reporting failure; the error destination retains the failed received envelope for operational recovery. Messages with no usable consumer can enter the skipped/dead-letter path.

These mechanisms are not the reliable store's quarantine. An operator must identify whether the problem is an outgoing intent, a received envelope, or a workflow instance before selecting a recovery operation.

## Cancellation

Caller cancellation, endpoint shutdown and a timeout are not interchangeable. A cancellation token can end the caller's wait without undoing a committed effect or removing a queued command. The filters distinguish expected cancellation associated with the operation from an unexpected cancellation exception.

For a request, response timeout belongs to the caller's waiting machinery. For a job, cancellation belongs to the job's coordination protocol. For a saga, a timeout can itself be a scheduled event. Reusing the word “timeout” does not unify their state transitions.

## Make retries safe

A consumer that can run twice needs a duplicate strategy. A transactional inbox can prevent repeated committed effects within its database boundary. External effects need business idempotency keys or reconciliation. A volatile outbox can discard outgoing operations from a failed local attempt, but does not survive process loss.

Use failure injection that targets your actual boundary: fail before commit, after commit but before acknowledgment, during dispatch, and after carrier acceptance while persisting success. Those scenarios exercise different guarantees.

## Source grounding

<!-- openwiki: broken internal link [../../src/ViciOne.ServiceBus/RetryPolicies] file "../../src/ViciOne.ServiceBus/RetryPolicies" does not exist. Fix the href or restore the target, then delete this comment. -->
The [retry filter](../../src/ViciOne.ServiceBus/Middleware/RetryFilter.cs), [retry policies](../../src/ViciOne.ServiceBus/RetryPolicies), and [receive pipeline](../../src/ViciOne.ServiceBus/Configuration/ReceivePipeConfiguration.cs) show exception selection and terminal handling.
