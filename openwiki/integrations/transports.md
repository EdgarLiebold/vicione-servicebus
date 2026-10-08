---
type: guide
title: "Choosing and operating a transport"
description: "Explain RabbitMQ, Azure, SQS, ActiveMQ, SQL, InMemory and riders with accurate capability boundaries."
tags: [servicebus, integrations]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-8373aee5a1598d73ed29d7f0
    resource: repo://docs/provider-capabilities.json
  - id: openwiki-source-0169be1b4cdad57067153ea8
    resource: repo://src/Transports/ViciOne.ServiceBus.AmazonSqs/AmazonSqsTransport/AmazonSqsReceiveLockContext.cs
  - id: openwiki-source-5655abd19a342f47b0c4dd8c
    resource: repo://src/Transports/ViciOne.ServiceBus.RabbitMq/DurableSend/RabbitMqDurableSendDispatcher.cs
  - id: openwiki-source-9dda337e007d6489aab7ac50
    resource: repo://src/Transports/ViciOne.ServiceBus.RabbitMq/RabbitMqTransport/RabbitMqSendTransportContext.cs
  - id: openwiki-source-fad1b7c8f7b1a3de83613b2b
    resource: repo://src/Transports/ViciOne.ServiceBus.SqlTransport/SqlReceiveLockContext.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Choosing and operating a transport

Transport choice determines physical routing, locks, acknowledgment, persistence and operational dependencies. ServiceBus gives common application interactions while each provider retains native carrier behavior. “Supported send” is therefore narrower than “supports every reliability and workflow combination.”

## Carrier models

| Provider | Messaging model | Operational boundary |
|---|---|---|
| InMemory | Process-local queues/exchanges | Process lifetime; no external durable broker |
| RabbitMQ | Exchanges, queues and bindings | Publisher acknowledgment and consumer settlement |
| Azure Service Bus | Queues, topics and subscriptions | Broker message locks and complete/abandon/dead-letter |
| Amazon SQS/SNS | Queue work and topic subscription fan-out | Visibility/receipt-handle settlement |
| ActiveMQ | Provider destinations and delivery acknowledgment | Session/protocol and broker behavior |
| PostgreSQL/SQL Server transport | Database carrier queues/topics | Native SQL delivery locks and database operations |
| Event Hubs | Rider with partitioned streams | Checkpointed progress; separate chapter |
| SignalR | Backplane over an owning bus | Node-local connections; separate chapter |

Provider packages supply their SDKs, configuration and topology. Core/Abstractions do not pull all broker SDKs into every application.

## RabbitMQ

Send routes to an exchange-form destination; receive endpoints declare the queue/bindings that accept work. Publication builds message-type topology and connects subscriber queues. Separate queues receive separate copies; processes sharing one queue compete.

Ordinary publisher acknowledgment depends on selected transport settings. The strong reliable dispatcher additionally requires an existing same-name durable quorum receive queue, persistent mandatory publishing, publisher confirmation and queue-equivalence checks before and after dispatch.

It rejects unsuitable destinations such as direct reply-to, alternate-exchange or queue-declaration forms that cannot establish that boundary. Administrative queue deletion/redeclaration remains outside the atomic publish proof, because the protocol does not supply a stable queue identity spanning those operations.

A failed post-confirm check may follow real delivery. Retrying retained intent can therefore duplicate work. See [Durable delivery](../reliability/delivery-and-quarantine.md).

## Azure Service Bus

Queues and subscriptions expose native lock and settlement behavior. The provider completes successful delivery and handles faults through abandon/dead-letter according to its configured pipeline. Long processing needs compatible lock/renewal settings.

Topics and subscriptions differ from RabbitMQ exchanges/bindings. Some topology options are frozen after evaluation; changing an SDK snapshot does not necessarily change configured declaration.

Native scheduling returns broker-assigned cancellation tokens. That capability cannot be substituted for caller-token saga request timeouts. Current ordinary Azure messaging support does not imply a unified durable-send dispatcher.

The repository has an emulator-backed integration profile and remaining native management/namespace work. Do not treat all passed local tests as evidence of every cloud deployment boundary.

## Amazon SQS/SNS

Queue sends use SQS; topic publication uses SNS. Receive locks are visibility periods tied to receipt handles. The provider renews visibility within configured bounds, deletes on completion and prepares redelivery after fault.

Long consumer work can exhaust renewal time. Changing visibility is not a database rollback. Duplicate-safe application effects remain necessary.

Region, scope, credential and queue/topic configuration must match the owning bus. Unified durable dispatcher support is currently absent even though ordinary send/publish/request exists.

## ActiveMQ and SQL transport

ActiveMQ supplies its native destinations, sessions and acknowledgments. Temporary queue/topic identities retain destination type; equal names do not make queue and topic registrations identical.

SQL transport uses carrier tables/functions and delivery locks rather than the EF application's reliable store. PostgreSQL and SQL Server packages provide their native implementations. A SQL receive lock is validated and renewed, and settlement can fail if ownership has been lost.

Sharing a database technology does not automatically enlist carrier and business operations in one application transaction. Current unified durable dispatcher support is absent for these carrier packages.

## InMemory

The fabric provides process-local routing, competing receivers and delayed mechanisms useful for tests and local hosts. It still exercises serialization/pipelines and must be configured with limits/endpoints.

Its reliable dispatcher waits for logical consumer completion before retiring retained intent. That is a process-local capability, not proof of broker persistence. An EF outgoing store can preserve intent, but the carrier itself remains volatile.

InMemory is not Mediator: it provides a transport fabric rather than direct local receive dispatch.

## Capability compatibility

The current [machine-readable matrix](../../docs/provider-capabilities.json) identifies ordinary operations and durable dispatcher support. RabbitMQ and InMemory have explicit reliable dispatch boundaries. Unsupported combinations fail composition; installing a persistence provider does not supply missing carrier proof.

Likewise, scheduling cancellation, saga repository queries and rider operations have their own capability contracts. Choose the combination required by the business guarantee, not only by a preferred cloud vendor.

## Deployment concerns

Separate entity declaration permissions from publishing/consuming permissions. Keep endpoint identity stable when retained queues matter. Provision the application schema before reliable writers; provision carrier topology in the intended ownership model.

Tune carrier admission and consumer execution separately. Check payload limits against the actual broker and serialized envelope. Expose endpoint readiness and reliability-store health independently.

For streaming and client connections, use [Event Hubs and SignalR](streaming-and-signalr.md); for application storage, use [Persistence](persistence-and-message-data.md).
