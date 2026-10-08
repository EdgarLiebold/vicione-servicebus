---
type: overview
title: "Glossary and capability guide"
description: "Define terms and provide practical scenario-to-chapter and package navigation without symbol dumps."
tags: [servicebus, reference]
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

# Glossary and capability guide

ServiceBus combines application-hosted messaging with independently selected workflow and persistence features. This glossary defines the concepts used across the wiki; it is not an API declaration list. For their relationships, read [the whole-system model](../architecture/conceptual-model.md).

## Communication and execution

| Term | Meaning |
|---|---|
| Bus | A configured messaging runtime owned by an application host; it selects a transport, limits and endpoints |
| Transport | The carrier integration that sends and receives messages, such as RabbitMQ or InMemory |
| Broker | An external system that owns queues and routing; ServiceBus itself runs inside applications |
| Contract | The data shape and identity that producers and consumers agree to exchange |
| Command | An instruction normally addressed to the owner that should act on it |
| Event | A statement of something that happened, published for independently interested subscribers |
| Send | Addressed delivery to a selected destination |
| Publish | Routing through the contract's publish topology to configured subscriptions |
| Receive endpoint | A transport destination plus its configured deserialization and processing pipeline |
| Consumer | Application code activated to handle a received contract |
| Consume context | The received message and its operation metadata, cancellation and contextual outgoing capabilities |
| Request | A command associated with reply correlation and a caller's waiting operation |
| Response | A reply matched to that request; it does not inherently describe all later business work |
| Fault | A failure-reporting message; it is different from retaining a failed envelope |
| Middleware | A filter composed around downstream processing; its position determines what it wraps |
| Scope | A DI lifetime for owned services; it does not by itself establish a database transaction |

Commands and events are business roles rather than different broker packet formats. Read [the messaging model](../learn/messaging-model.md) and [send/publish](../messaging/send-and-publish.md) for their routing consequences.

## Reliability and ownership

| Term | Meaning |
|---|---|
| Admission | Acceptance of content into the selected capture/storage boundary after its required validation |
| Envelope | Serialized message body plus transport-independent metadata carried to the receive runtime |
| Payload limit | A bound enforced on the body, envelope or related admission dimension |
| Carrier acceptance | The dispatcher's supported evidence that the transport accepted an outgoing message |
| Consumer completion | Completion of receive processing at the boundary supported by that integration |
| Acknowledgment/settlement | The transport's action ending or updating ownership of a received delivery |
| Outbox | Outgoing capture; persistent transactional capture and volatile buffering have different promises |
| Inbox | Stored identity and ownership evidence used to avoid repeating committed consumer effects |
| Idempotency | A business operation producing the intended result safely when requested repeatedly |
| Lease | Time-limited ownership of stored work |
| Fencing | Rejecting actions from an obsolete owner after ownership has changed |
| Retry | Another attempt within the mechanism that owns a failure |
| Redelivery | A later receive attempt, normally with a new processing operation |
| Quarantine | Retained reliable-store failure requiring an explicit recovery decision |
| Error destination | Retained failed receive envelopes after terminal receive processing |
| Abandon | An explicit terminal retained inbox decision; it is not an outgoing abandonment operation |
| Retention | How long state remains; configured policy and implemented cleanup must be distinguished |
| Capacity | A bound on the state or execution resources actually counted by its owner |

An outbox admission, broker acknowledgment, consumed counter and committed business transaction describe different boundaries. The [message lifecycle](../architecture/message-lifecycle.md) connects them. The [inbox](../reliability/inbox-and-idempotency.md) and [outbox](../reliability/transactions-and-outbox.md) explain which effects can commit together.

## Coordination models

| Term | Meaning |
|---|---|
| Correlation | Mapping related events to a request or persistent conversation instance |
| Saga | Repository-backed conversation state changed by successive messages |
| State machine | Explicit states, events and transition behaviors for a conversation |
| Routing slip | Courier's traveling itinerary with arguments, variables and activity history |
| Activity | One independently hosted Courier execution step |
| Compensation | A defined business countereffect for a completed activity |
| Future | A saga-based asynchronous command whose terminal result/fault remains requestable |
| Job | Long execution coordinated with capacity allocation and supervised attempts |
| Schedule | Work eligible after a due time under a selected scheduler's ownership |
| Recurrence | A rule and state that creates successive executions; support depends on the selected model |

A saga repository, a reliable store and a scheduler are separate state owners even when they use the same database technology. [Persistence](../integrations/persistence-and-message-data.md) explains those integrations.

## Provider selection

Ordinary send/publish capability is broader than reliable durable-dispatch capability. RabbitMQ and InMemory have the reliable dispatcher integrations discussed here; the other carriers must be evaluated by their declared capabilities.

InMemory transports messages through a local messaging fabric. [Mediator](../integrations/mediator-and-multibus.md) performs process-local dispatch without broker queues. Event Hubs introduces stream partitions and checkpoints; [SignalR](../integrations/streaming-and-signalr.md) uses backplane messages to connect node-local hub state. These are different communication models.

For package decisions, use [packages and ownership](../architecture/packages-and-ownership.md). For current contradictory or disconnected guarantees, use [implementation gaps](implementation-gaps.md).

## Source grounding

The [repository overview](../../README.md), [API surface description](../../docs/api-surface.md), and [provider capabilities](../../docs/provider-capabilities.json) ground the terminology and capability distinctions.
