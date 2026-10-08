---
type: guide
title: "Scheduling and cancellation"
description: "Explain due records, adapter choice, recurring schedules, tokens, saga timeout and replacement constraints."
tags: [servicebus, workflows]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-04a59b9369fd2c0481004ec6
    resource: repo://src/ViciOne.ServiceBus.Sagas/SagaStateMachine/Activities/RequestActivityImpl.cs
  - id: openwiki-source-906a5e75db9d003121c0b565
    resource: repo://src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Scheduling and cancellation

Scheduling makes work eligible at a later time. It does not guarantee execution at an exact clock instant: dispatch capacity, broker availability and consumer load can delay processing after the due time.

A schedule has a payload, destination, due time and token. Its owner can be a reliable store, a broker's native scheduler or a scheduler service reached by messages. Selecting that owner determines persistence, cancellation and recurring support.

## One-time reliable scheduling

The reliable scheduler creates an outgoing intent with `DueAt` and a generated durable-send identifier. The reliable worker delivers it once eligible. Admission can be local transactional capture before commit; receiving a scheduled-message handle does not prove future carrier acceptance or consumption.

The store's cancellation operation returns a typed disposition. A missing intent or one that has progressed beyond cancellation can fail the scheduler cancellation call. Cancellation races with claiming and delivery: a message already accepted by a carrier cannot be assumed retractable through a store operation.

The same serialized payload and admission limits apply to scheduled work. A distant due time does not exempt the record from retained outgoing capacity.

## Scheduled publication currently differs

The general message scheduler resolves publication through bus publish topology. The reliable scheduler currently resolves `SchedulePublishAsync` through a configured send route and forwards to scheduled send. It can require a route where ordinary publication needs none, and a route can target a single queue rather than subscribers.

This is [SB-ARCH-05](../reference/implementation-gaps.md). Do not assume switching to reliable scheduling preserves ordinary event-publication semantics without verifying the supported destination and topology.

## Scheduler adapters and token ownership

A scheduler endpoint carries schedule/cancel commands to a service. Quartz can supply that service and recurring operations. Its lifecycle must have one owner: the integration guards against also installing an independent Quartz hosted service.

A native transport scheduler may return a provider-assigned token. A delayed-message adapter can accept caller-specified identity while lacking cancellation. These differences are represented as cancellation capabilities, not hidden behind identical method names.

| Capability | Practical meaning |
|---|---|
| Caller-specified token | The caller can establish identity before scheduling and use the supported cancellation protocol |
| Caller-specified token without cancellation | Identity is predictable, but cancellation cannot be relied on |
| Provider-assigned token | The scheduling provider determines identity after acceptance |
| Unknown | Code requiring stronger semantics must not infer them |

Always use the capability of the configured adapter and the operation in question. Ordinary scheduling support does not automatically establish that a saga's timeout protocol can use it.

## Saga timeouts

A saga request with a positive timeout checks for the required caller-specified cancellation capability before sending. Its request identifier must match the timeout's identity. This avoids issuing business work and then discovering that the timeout protocol cannot safely represent it.

Cancellation and replacement also interact with messages already being processed. With a non-cancellable mechanism, safe replacement must distinguish the matching current timeout delivery from unrelated or stale events. A late timeout must not terminate a later request accidentally.

The timeout event begins another saga transition; it is different from a caller's `GetResponseAsync` wait. Read [requests](../messaging/request-and-response.md) and [sagas](sagas.md) together.

## Recurrence has distinct implementations

Quartz recurring scheduling and JobService recurring jobs have working, separate coordination models. They own recurrence through their own state and lifecycle.

The reliable EF model also maps a recurring schedule record and the reliability documentation describes store-owned recurrence. No connected creation/advancement runtime was found for that model. This is [SB-ARCH-02](../reference/implementation-gaps.md). A mapped table does not make reliable recurring scheduling available.

## Operational design

Specify the clock, timezone policy for recurrence, destination, token owner and missed-run behavior. Keep one-time store records, Quartz jobs and JobService jobs conceptually separate. Verify restart, cancellation races and repeated delivery against the selected implementation.

## Source grounding

The [reliable scheduler](../../src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs), [general scheduler](../../src/ViciOne.ServiceBus/Scheduling/MessageScheduler.cs), and [saga request activity](../../src/ViciOne.ServiceBus.Sagas/SagaStateMachine/Activities/RequestActivityImpl.cs) establish these boundaries.
