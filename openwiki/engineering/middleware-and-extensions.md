---
type: guide
title: "Pipelines, middleware and extension contracts"
description: "Explain filter composition, exception selection, timeout/circuit/rate control, transforms, initializers, provider contracts, observers and extension lifecycle."
tags: [servicebus, engineering]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-b7ba85071e1384bdcb2545e2
    resource: repo://src/ViciOne.ServiceBus/Configuration/TransformConfigurationExtensions.cs
  - id: openwiki-source-acf082b7a71a717cd8ac85bb
    resource: repo://src/ViciOne.ServiceBus/MessageData/Values/DeserializedMessageData.cs
  - id: openwiki-source-e232b32d9c307f7f60f63287
    resource: repo://src/ViciOne.ServiceBus/Middleware/RetryFilter.cs
  - id: openwiki-source-bf13ff5cf7cc0386c2bb9edc
    resource: repo://src/ViciOne.ServiceBus/Serialization/Admission/PayloadAdmissionTransportBoundary.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Pipelines, middleware and extension contracts

Pipelines make cross-cutting behavior composable. A filter receives a context and the next pipe, performs work around that call and awaits its completion. Optional capabilities extend this infrastructure rather than replacing every transport or consumer.

## Filter order defines the operation

A filter outside retry can observe one logical operation; a filter inside retry can run on every attempt. A scope outside a repeated stage may be reused while a scope inside it may be recreated. An outbox placed within retry can discard failed-attempt effects before the next attempt.

```mermaid
flowchart LR
    A["Incoming delivery"] --> R["Retry"]
    R --> O["Attempt-local outgoing capture"]
    O --> S["Scope / component"]
    S --> B["Business behavior"]
```

This is an illustrative arrangement, not a mandatory universal ordering. Actual configurators and specialized component observers install the chosen pipe specifications. Diagnose the resulting order before adding a new filter.

## Resilience controls solve different problems

**Retry** re-invokes downstream work after selected failures. **Delayed redelivery** schedules a later delivery. **Timeout** supplies a linked cancellation boundary. **Circuit breaker** limits access to an unhealthy downstream stage according to its failure sample and probe state. **Rate limiting** paces operations, while **concurrency limiting** bounds simultaneous work.

A timeout cannot forcibly roll back external effects or kill arbitrary user code. Work must honor cancellation. Circuit breaker state is runtime-local; it is not a persistent distributed lock. Rate limits do not establish per-key ordering.

Technical retry helpers compose the repository's canonical short retry and delayed policy through a classifier. Exception selection walks structural exception chains/aggregate branches; ignore matches can veto broader handling. Preserve the original operation failure when reporting or cleanup also fails.

Read [Failures](../reliability/failures-and-retries.md) and [Concurrency](../messaging/concurrency-and-batching.md) for resource and ordering consequences.

## Contexts carry capability and ownership

Pipe contexts contain payloads used to attach scope, time, diagnostics or specialized provider capabilities. Consume contexts add typed message metadata and completion tracking. Wrappers can change outgoing endpoints while preserving receive metadata.

A wrapper must forward the capability actually required by downstream code. Scheduler cancellation capability, for example, determines whether saga timeouts can be safely canceled; silently losing that capability through an outbox wrapper changes behavior.

Do not store delivery-owned contexts in long-lived singletons. Their scope, cancellation, lock and completion belong to one operation.

## Transformations and MessageData

Consume, send and publish transforms can construct changed message views through property providers and converters. Their placement determines whether a transformation occurs before business logic or serialization. A transform is not automatically a schema-migration service.

MessageData uses send/consume transformations to store or resolve external values. A deserialized reference can carry an address while throwing on `Value` until the consume transform loads it. Repository accessibility and blob lifetime are therefore part of the operation.

## Initializers and application APIs

The normal application path uses concrete typed messages and option records. Advanced initializers construct interface contracts from values and conventions, with the optional Initializers package adding specialized variables and overloads.

Anonymous-value convenience does not weaken contract identity or serialization requirements. Use it only when its construction and completion behavior are understood. The originating MassTransit API shape is not authoritative for the current surface.

## Registration extensions

Component kinds contribute registration discovery, endpoint requirements and specialized configuration. Core supplies ordinary consumer behavior; capability packages add saga, activity, future and job behavior. The registration context selects owners and groups components into endpoints.

A new kind must cooperate with bus identity, scope ownership, endpoint topology and testing observation. Adding a public interface without a connected runtime path does not create a working capability.

## Serializer and provider extensions

A serializer extension must integrate exact-byte payload admission, immutable envelope/body evidence, supported contract metadata and configured content type. The physical boundary rejects serializers that cannot establish complete admission.

A transport integration owns SDK configuration, addressing, topology, send/receive contexts, settlement and lifecycle. A durable dispatcher additionally declares an acceptance boundary and returns an explicit completion mode. Ordinary transport support is insufficient evidence for reliable dispatch.

A persistence provider supplies atomic admission, bounded outgoing capacity, claims/leases, fenced transitions and operational queries according to the contracts it implements. Current built-in scope limitations are recorded in [Implementation gaps](../reference/implementation-gaps.md).

## Observers and diagnostics

Observers can attach at send, publish, consume, receive or lifecycle boundaries. Their callbacks must follow the actual contract; generic observer exceptions can remain part of operation failure handling. The specifically hardened OpenTelemetry and journal paths contain observational failures so they do not rewrite delivery outcomes.

Do not assume every arbitrary observer is harmless merely because telemetry is observational. Disconnect handles and owned resources also need disposal.

## Inspecting and validating an extension

Probe output describes the runtime graph. StateMachineVisualizer renders state/event/exception relationships as Mermaid or Graphviz. Analyzers detect certain dangerous application shapes; tests establish actual execution.

Validate an extension with both successful and failing paths, including cleanup, cancellation and ownership conflicts. Use the public package boundary when the extension is meant for package consumers. See [Build and testing](build-and-testing.md).

Source: [retry filter](../../src/ViciOne.ServiceBus/Middleware/RetryFilter.cs), [timeout filter](../../src/ViciOne.ServiceBus/Middleware/TimeoutFilter.cs), [transform configuration](../../src/ViciOne.ServiceBus/Configuration/TransformConfigurationExtensions.cs) and [admission boundary](../../src/ViciOne.ServiceBus/Serialization/Admission/PayloadAdmissionTransportBoundary.cs).
