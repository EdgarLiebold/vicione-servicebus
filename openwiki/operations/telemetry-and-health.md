---
type: guide
title: "Telemetry, readiness and health"
description: "Explain signal meaning, activation ownership, exporting, correlation and diagnostic interpretation."
tags: [servicebus, operations]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-d7644d9029aaf56c189e640b
    resource: repo://docs/observability.md
  - id: openwiki-source-04cdc1ecb7d0dbc11ea59d51
    resource: repo://src/ViciOne.ServiceBus/Monitoring/Telemetry/ServiceBusInstrumentation.cs
  - id: openwiki-source-361d3d2ee0c5e4ced1fe42b1
    resource: repo://src/ViciOne.ServiceBus/Operations/ReliableMessaging/DurableSenderHealthCheck.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Telemetry, readiness and health

Observability answers three different questions: can this process receive traffic, is messaging progressing, and what happened to a particular operation? Endpoint readiness, aggregate metrics and traces serve those questions respectively. None proves that every business operation completed successfully.

## Who owns collection

ServiceBus emits signals through .NET diagnostics. The application owns sampling, collection and exporters. Both the meter and activity source are named `ViciOne.ServiceBus`. Select that name in your telemetry configuration; an exporter is not installed merely by adding the bus.

DI registration uses the application's `IMeterFactory` and does not replace an existing factory. Multiple buses in one service provider share its metrics scope; independent providers have independent scopes. A bus built outside DI opts into instrumentation explicitly. This matters in tests and applications that construct several hosts: signals must not borrow a factory from a previously disposed provider.

The framework treats listeners as observational. Listener failures must not change delivery, retries or outbox decisions. A missing trace is therefore a diagnostic problem rather than evidence that no message was sent.

## Read metrics according to their boundary

A send counter counts attempts, including failed attempts. A consumed counter counts delivery into the receive pipeline. Neither is a unique business-success counter. Repeated delivery and retries can produce multiple observations for the same business command.

Enqueue and delivery are separate outbox outcomes. Successful enqueue means an outgoing intent was accepted into its capture or storage boundary; later delivery can still fail. Interpret backlog gauges together with delivery outcomes and oldest-pending age. Growing stored count with few delivery successes suggests a carrier or worker problem; a stable count with increasing age may indicate particular stuck records.

Durations are measured in seconds. Consumer processing duration and carrier delivery duration measure different work. The active-operation instrument is an up/down counter and describes currently executing work, not queue depth.

Metric dimensions are deliberately bounded: transport, operation, processor kind and outcome. Message identifiers, endpoint addresses, message types and bodies are not metric dimensions. Use tracing and appropriately controlled structured logs to investigate individual operations.

## Two health surfaces

The bus health surface describes runtime and endpoint readiness. The reliable-messaging health check queries the outgoing store snapshot. An unreachable store or storage beyond its configured hard bounds is unhealthy. Quarantine, exhausted capacity or a sufficiently old pending record produces degraded health.

Retry-scheduled count is included in the snapshot data, but there is no independent retry-count threshold in the health decision. Applications should define alarms that fit their workload rather than interpreting every exposed count as a separate unhealthy condition.

A healthy outgoing snapshot does not establish that the inbox is small, the saga repository is reachable, or external business effects are correct. In particular, the current capacity and retention discrepancies described under [implementation gaps](../reference/implementation-gaps.md) prevent treating this check as a bound on all reliable-store tables.

## Diagnose an operation

Begin with its actual failure boundary. A producer exception before admission differs from an admitted intent waiting for carrier acceptance. A broker delivery accepted by the receive pipeline differs from a committed consumer transaction. A request timeout describes the caller's wait; it does not prove that the command was never executed.

Follow the operation through [the message lifecycle](../architecture/message-lifecycle.md), then use [delivery and quarantine](../reliability/delivery-and-quarantine.md) for retained work or [the journal](message-journal.md) for optional diagnostic capture. Keep trace retention, journal retention and reliable-state retention separate: they belong to different owners and have different correctness implications.

Application health publication is also an application decision. A degraded backlog may justify an alert while leaving existing consumers running so they can drain it. Align orchestrator readiness and restart behavior with that recovery goal.

## Source grounding

The [observability contract](../../docs/observability.md) defines instruments and ownership. The [reliable health implementation](../../src/ViciOne.ServiceBus/Operations/ReliableMessaging/DurableSenderHealthCheck.cs) shows the exact decision conditions.
