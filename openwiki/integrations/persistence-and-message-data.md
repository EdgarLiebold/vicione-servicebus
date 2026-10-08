---
type: guide
title: "Persistence and large message data"
description: "Separate reliability storage, saga repositories, journals and external payload blobs; explain deployment."
tags: [servicebus, integrations]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-78887165d92736328c0ec0ae
    resource: repo://docs/migrations/README.md
  - id: openwiki-source-e7facb2fca8ac06f13f970eb
    resource: repo://src/Persistence/ViciOne.ServiceBus.Azure.Table/Saga/AzureTableSagaRepositoryContextFactory.cs
  - id: openwiki-source-80224ac48b354aad6590b22b
    resource: repo://src/Persistence/ViciOne.ServiceBus.DynamoDb/Saga/DynamoDbSagaRepositoryContextFactory.cs
  - id: openwiki-source-0cd32ba41e193557455ea48d
    resource: repo://src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableMessagingServiceCollectionExtensions.cs
  - id: openwiki-source-acf082b7a71a717cd8ac85bb
    resource: repo://src/ViciOne.ServiceBus/MessageData/Values/DeserializedMessageData.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Persistence and large message data

Persistence is not one interchangeable capability. ServiceBus stores different kinds of state for different reasons: outgoing intent, processed-message identity, saga instances, external payloads and diagnostic journal entries. Choose a provider by the state and atomicity boundary it implements.

## Persistence roles

| Role | Meaning | Typical implementation here |
|---|---|---|
| Reliable outbox/inbox/single due intent | Recovery of messaging decisions and effects | Unified EF store; process-local InMemory implementation |
| Saga state | A correlated process instance | EF Sagas, DynamoDB, Azure Table, InMemory |
| MessageData | Bytes externalized from the message | S3, Azure Blob, filesystem, InMemory; optional encryption wrapper |
| Journal | Policy-selected diagnostic projection | EF Core or Azure Table integration |
| SQL carrier entities | The transport's messages/queues | SQL transport provider, distinct from EF application outbox |

An S3 object repository is not a reliable inbox. A SQL transport table is not automatically the application outbox. A saga repository does not make every arbitrary consumer effect transactional.

## EF reliable storage and the application database

The unified EF integration uses an application DbContext registered through an `IDbContextFactory<TContext>`. Map its records in the model:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddViciOneReliableMessaging();
}
```

The mapped tables include outgoing records, inbox records and an outgoing capacity ledger. A recurring-schedule table is also mapped, but there is no connected store-backed recurrence runtime; treat that as an [implementation gap](../reference/implementation-gaps.md).

The application owns migrations, provider selection, schema name, backup and rollout. Registration does not create/update tables. Deploy schema before writers start. The repository ships no general MassTransit database import.

Within the supported consume transaction, business rows, outgoing intents and consumed inbox identity can commit together. Outside a consumer, the transactional session must commit its owning DbContext; a caller-owned outer transaction still needs the caller's commit.

## Saga repositories

Saga correlation locates a process instance; the repository determines concurrency and persistence. InMemory serializes local instance access but loses state on restart. EF supports repository load/query capabilities and configured locking/transaction strategy.

DynamoDB and Azure Table expose key-based load/dispatch and reject property-query correlation. Choosing either requires event correlation compatible with that capability. They cannot emulate every EF query simply because they implement saga dispatch.

Repository state persistence is also distinct from outgoing-message atomicity. If the repository and business/outbox DbContext are separate owners, do not assume one transaction includes both.

## MessageData: keeping large bytes out of the envelope

A MessageData contract carries an external reference or inline value. Send transformations can store data; consume transformations resolve it through the configured repository. The referenced bytes and their address have their own lifetime.

Example excerpt:

```csharp
public sealed record DocumentReady(
    Guid DocumentId, MessageData<string> Document);

MessageData<string> data =
    await repository.PutStringAsync(text, cancellationToken);
var message = new DocumentReady(documentId, data);
```

Use `ViciOne.ServiceBus.Advanced.Serialization` for the repository contract. Sending this message still requires normal bus composition and compatible MessageData consumption.

The receiver needs access to the referenced storage. Credentials, endpoint accessibility and encryption settings must match the intended deployment. A URI in a valid message does not prove the blob is available.

## Payload lifetime and transaction limits

Store payload data long enough for broker delays, retries, redelivery and operational recovery. Deleting a blob immediately after an initial consume can break a later duplicate or another subscriber. A short broker TTL does not by itself define every subscriber's blob lifetime.

Blob upload and database intent commit are separate operations. Failed admission can leave an unused blob; a transaction rollback does not automatically roll back object storage. Applications need an explicit lifecycle/cleanup strategy.

A deserialized MessageData reference throws on `Value` until the consume transformation has loaded it. This distinguishes “the address was decoded” from “the value is available.”

## Admission and offload

Body and envelope limits apply to actual serialized bytes. The optional offload threshold requires observed stored-reference evidence; it does not automatically transform every large arbitrary object into a blob.

Use a MessageData-compatible contract and repository configuration. The envelope containing the reference still needs to fit limits. The S3 deployment guide explains its provider-owned options and policies; do not substitute an unbounded payload by raising every carrier limit.

## Journal storage

Journal stores retain selected projections under classification and finite entry/count/retention limits. They do not replace reliable intent or saga state. A metadata-only policy can make a useful operational journal without persisting sensitive bodies.

## Existing data and current discrepancies

Stable bus and consumer identities matter for retained inbox rows. The separate classic EF model has an explicit identity migration guide. Its schema and operations differ from unified reliable storage.

The required unified retention duration currently has no connected cleanup consumer, and advertised shared capacity does not bound retained inbox state. Plan actual database growth accordingly; do not infer seven-day cleanup from a configured duration.

Source: [schema deployment](../../docs/migrations/README.md), [EF registration](../../src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableMessagingServiceCollectionExtensions.cs), [DynamoDB repository](../../src/Persistence/ViciOne.ServiceBus.DynamoDb/Saga/DynamoDbSagaRepositoryContextFactory.cs), [Azure Table repository](../../src/Persistence/ViciOne.ServiceBus.Azure.Table/Saga/AzureTableSagaRepositoryContextFactory.cs) and [S3 guide](../../docs/amazon-s3-message-data.md).
