---
type: overview
title: "Start here: understanding and using ServiceBus"
description: "Introduce the learning path, terminology, first runnable example, and task navigation."
tags: [servicebus, quickstart.md]
sources:
  - id: openwiki-source-8373aee5a1598d73ed29d7f0
    resource: repo://docs/provider-capabilities.json
  - id: openwiki-source-23775c3de52f3ab95a13cb8b
    resource: repo://README.md
  - id: openwiki-source-9b5701a4f3c4f34c0f1dcc70
    resource: repo://samples/SuiteComposition/Program.cs
generated: { by: "codex", at: "2026-10-07T17:44:57.086Z" }
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:44:57.086Z
---

# Start here: understanding and using ServiceBus

ViciOne.ServiceBus is an application-hosted messaging framework. Your process registers contracts and consumers, creates a bus, and connects receive endpoints to a chosen transport. The broker carries messages between processes; ServiceBus supplies the runtime that routes, deserializes and executes them.

The system also contains coordination and persistence features. Requests associate replies with a waiting caller. Sagas persist the state of conversations. Courier executes an itinerary with compensation. Futures retain asynchronous outcomes. JobService coordinates execution capacity and attempts. Reliable messaging captures outgoing intents and duplicate-safe receive state. These features cooperate through the messaging runtime, but each owns a different kind of state.

## Learn the complete model first

Read [the whole system](architecture/conceptual-model.md) for its components, relationships and ownership boundaries. Then read [the messaging model](learn/messaging-model.md) for commands, events, queues and delivery semantics. These two chapters explain why a message can be accepted by a broker while the business operation is still incomplete.

[Your first application](learn/first-application.md) supplies a complete small host with a consumer and request/response interaction. Its purpose is to make startup, endpoint addressing and consumer execution tangible. It does not substitute for the system model or demonstrate production durability.

Next, follow [a message through the runtime](architecture/message-lifecycle.md). This connects producer APIs, serialization, transport, receive scopes, middleware, outgoing work and settlement.

## Find the right chapter

| Your question | Read |
|---|---|
| How do I choose packages and compose a host? | [Packages](architecture/packages-and-ownership.md), [bus and host](configuration/bus-and-host.md) |
| Where does a command or event go? | [Topology](configuration/endpoints-and-topology.md), [send and publish](messaging/send-and-publish.md) |
| What happens when a request times out? | [Requests](messaging/request-and-response.md) |
| How do consumers use scoped services? | [Consumers and scopes](messaging/consumers-and-scopes.md) |
| How do contracts and size limits work? | [Contracts and serialization](messaging/contracts-and-serialization.md) |
| How do I control load? | [Concurrency and batching](messaging/concurrency-and-batching.md) |
| Which retry mechanism applies? | [Failures and retries](reliability/failures-and-retries.md) |
| How do database effects and outgoing messages commit together? | [Transactions and outbox](reliability/transactions-and-outbox.md) |
| How do duplicate deliveries affect business effects? | [Inbox and idempotency](reliability/inbox-and-idempotency.md) |
| How do I inspect retained failures? | [Delivery and quarantine](reliability/delivery-and-quarantine.md) |
| How do delayed work and long conversations work? | [Scheduling](workflows/scheduling.md), [sagas](workflows/sagas.md) |
| How do distributed workflow models differ? | [Courier](workflows/courier.md), [futures](workflows/futures.md), [jobs](workflows/jobs.md) |
| Which provider supports my required guarantees? | [Transports](integrations/transports.md), [persistence](integrations/persistence-and-message-data.md) |
| How do local dispatch, multiple buses and streaming fit? | [Mediator and multibus](integrations/mediator-and-multibus.md), [streaming and SignalR](integrations/streaming-and-signalr.md) |
| How do I operate and extend it? | [Telemetry](operations/telemetry-and-health.md), [deployment](operations/deployment-and-troubleshooting.md), [middleware](engineering/middleware-and-extensions.md), [build and testing](engineering/build-and-testing.md) |

## Understand the guarantees you select

A transport, a transactional store and a workflow repository solve different problems. Choosing a durable broker does not make a business database update atomic with a send. Choosing a saga repository does not automatically enable an inbox. Choosing an in-memory mechanism does not make its state survive process loss.

For production composition, identify each state owner explicitly: business database, outgoing store, inbox, broker queues, saga repository, scheduler and diagnostic store. Decide which failures must be recoverable and then select the integration that supplies that boundary.

<!-- openwiki: broken internal link [</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>] file "</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>" does not exist. Fix the href or restore the target, then delete this comment. -->
The current implementation has [confirmed concept and implementation discrepancies](reference/implementation-gaps.md). The documented unified reliability model coexists with a separate public EF transactional model; reliable-store recurrence has no connected runtime; terminal retention has no cleanup consumer; shared capacity bounds only count outgoing records; and reliable scheduled publication resolves a send route rather than publish topology. The separate [inconsistency register](</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>) records evidence, consequences and required dispositions for all five findings.

Use the [glossary](reference/glossary.md) when a term is unfamiliar. Code excerpts in topic chapters are marked as excerpts; the first-application chapter provides the complete introductory program.

## Source grounding

The [repository overview](../README.md), [provider capabilities](../docs/provider-capabilities.json), and [composition sample](../samples/SuiteComposition/Program.cs) establish the application's runtime and provider boundaries.
