---
type: guide
title: "Deployment and troubleshooting"
description: "Provide symptom-to-cause runbooks, schemas, startup, backlog, quarantine and recovery."
tags: [servicebus, operations]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-78887165d92736328c0ec0ae
    resource: repo://docs/migrations/README.md
  - id: openwiki-source-b7b46b617fe6484005594ea5
    resource: repo://src/ViciOne.ServiceBus/Operations/ReliableMessaging/ReliableMessagingOperations.cs
  - id: openwiki-source-cf5c38200f10fd08caa66bd3
    resource: repo://src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableMessagingDeliveryService.Delivery.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Deployment and troubleshooting

Deployment connects the package graph, carrier topology, application schema and runtime policy. Troubleshooting identifies the boundary that failed. Start from observed state rather than assuming every missing business event is a transport outage.

## Before starting writers

Use the approved locked application graph and matching .NET runtime. Apply application-owned EF migrations before reliable messaging starts. Registration does not create tables.

Configure one transport and explicit limits per bus. Select compatible reliability store/dispatcher, stable contract catalog, finite outgoing capacity and delivery policy. Give typed retained-state buses stable persistence identities.

Provision carrier entities and permissions under the intended ownership model. Endpoint naming changes can create new entities and leave old work behind. Check scheduler and saga repository capability combinations separately.

The configured unified retention value currently does not perform terminal cleanup, and shared advertised bounds do not cover retained inbox state. See [Implementation gaps](../reference/implementation-gaps.md) before making database growth assumptions.

## Startup and readiness

Wait for bus startup when the application needs readiness failures during host startup. Expose endpoint and store health separately: a reachable broker with an unavailable database is not a healthy reliable service, and a reachable database with a growing backlog is not successful delivery.

Shutdown should stop admission, allow bounded owned processing and await resource cleanup. A consumer detached task is not drained by ordinary pipeline completion. Hard process loss can replay committed intents and lose volatile work.

## Symptom map

| Symptom | Boundary to inspect |
|---|---|
| Startup fails before broker connection | Composition ownership, selected options and schema |
| Host runs but no receiving endpoint | Background start result and endpoint registration |
| Send succeeds, consumer never runs | Destination, topology, contract/content type and endpoint |
| Requests time out | Request/reply path, consumer behavior, capture/commit and deadline |
| Outgoing pending age grows | Worker state, dispatcher failures, carrier proof and due/lease times |
| Quarantine grows | Failure classification, attempt exhaustion and contract validity |
| Duplicated business effects | Message/consumer identity and external-effect idempotency |
| Large-message rejection | Actual body/envelope size, offload owner and carrier native bound |
| MessageData cannot load | Repository access, consume transformation and blob lifetime |
| Saga messages go missing/fault | Correlation, initiation/missing policy and repository capability |

## Startup configuration failures

Read the feature/bus name and corrective instruction in the exception. Confirm limits and transport have one owner. For reliable messaging, check catalog, outbox/inbox/schedule store and dispatcher ownership.

A positive configuration test reaching a pre-runtime sentinel does not prove broker connectivity. Conversely, changing credentials cannot fix a duplicate owner or a missing reliable dispatcher.

Apply schema through deployment, not by adding automatic destructive database creation to production startup.

## Work not reaching a consumer

Record the selected bus and resolved destination. Compare them with the receive input address and configured naming formatter. Verify publication subscriptions and matching envelope message types.

Confirm the consumer was registered and endpoints were actually configured. A type that compiles but has no endpoint does not consume. Inspect dead-letter/error destinations to distinguish no-match from execution failure.

Use tracing/log correlation rather than high-cardinality metric labels. Do not put full message bodies into generic health endpoints.

## Requests and uncertain completion

Check whether the request was sent directly, buffered or captured in an outbox. A local call completing after capture is not dispatch. Verify commit/flush and reply endpoint readiness.

A timeout can occur after business effects committed. Query by the logical operation identity or use an idempotent retry strategy instead of assuming safe resubmission. Distinguish request faults from explicit negative business responses.

## Reliability backlog

A record can be pending, claimed, awaiting process-local completion, retry-scheduled or quarantined. Read the selected store's snapshot and bounded quarantine pages.

Check due time before diagnosing starvation. Check leases/generation fencing before manually changing ownership. Lease expiry allows recovery; blindly deleting a claimed record can race a worker.

Transient carrier failures can schedule persisted retry. Unclassified/permanent failures and exhausted budgets can quarantine. Persistence failure after successful dispatch can replay a previously delivered message; duplicate-safe effects are necessary.

## Recovery decisions

Use `IReliableMessagingOperations<TBus>` for the unified model's bounded queries and typed actions. Requeue makes quarantined work eligible again, discard removes supported quarantined state, and inbox abandonment records deliberate terminal non-processing.

Abandon is not supported for an outgoing reference by the current operations implementation. Dispositions such as not-found/invalid-state must be handled; an action name does not guarantee it changed anything.

The separate EF transactional model has another operations surface. Unified queries are not an aggregate view of both models.

Applications own authorization, review of business consequences and external audit/UI integration. The wiki does not prescribe deleting raw database rows.

## Diagnostics can disagree without corruption

A journal policy can filter an observation, an append can fail, and metrics can be disabled. Absence of a diagnostic record is not proof of absence of a business effect. A send counter records attempts, not unique committed outcomes.

Compare the state owner relevant to the question: business database, reliable intent, inbox record, saga repository, carrier or blob store. They represent different boundaries.

Source: [schema deployment](../../docs/migrations/README.md), [operations implementation](../../src/ViciOne.ServiceBus/Operations/ReliableMessaging/ReliableMessagingOperations.cs), [delivery outcomes](../../src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableMessagingDeliveryService.Delivery.cs) and [health](../../docs/observability.md).
