---
type: guide
title: "Contracts, serialization and payload limits"
description: "Explain message identity, envelopes, schema evolution, formats and admission limits."
tags: [servicebus, messaging]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-c0a93de17ded86a723259d6d
    resource: repo://src/ViciOne.ServiceBus.Abstractions/Contracts/MessageContractIdentity.cs
  - id: openwiki-source-763f12ee841996c8cd4192fe
    resource: repo://src/ViciOne.ServiceBus/Advanced/MessageContractCatalogBuilder.cs
  - id: openwiki-source-9a4f53b8147fdc37e064bfab
    resource: repo://src/ViciOne.ServiceBus/DependencyInjection/MessageContractServiceCollectionExtensions.cs
  - id: openwiki-source-bf13ff5cf7cc0386c2bb9edc
    resource: repo://src/ViciOne.ServiceBus/Serialization/Admission/PayloadAdmissionTransportBoundary.cs
  - id: openwiki-source-49817d0f43fa653fe8d9da99
    resource: repo://src/ViciOne.ServiceBus/Serialization/Serializers/SystemTextJsonMessageSerializer.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Contracts, serialization and payload limits

Contracts define meaning and compatibility. Serialization defines how their data and metadata cross a wire or persistence boundary. Limits constrain the actual encoded representation. Changing any of these can affect routing, durable replay and consumers even when local code still compiles.

## Design contracts as messages

Use reference-type contracts—records/classes or supported interfaces—with the data needed by receivers. Avoid embedding DbContext entities, service instances or live process resources. The receiver reconstructs data rather than sharing the producer's object identity.

Examples:

```csharp
public sealed record SubmitOrder(Guid OrderId, Guid CustomerId);
public sealed record OrderSubmitted(Guid OrderId, DateTimeOffset SubmittedAt);
```

The first expresses work for an owner; the second describes an outcome. Identifiers are business data when needed for behavior, while envelope metadata supplies causal routing.

## Two identity layers

Envelope message-type identifiers describe CLR contracts supported by the serialized message. They participate in typed dispatch and topology.

Reliable persistence additionally uses `MessageContractIdentity`: a stable name and major version such as `orders.submit;v=1`, deliberately excluding assembly name/version/public-key token. An immutable catalog maps identity to runtime type.

```csharp
reliable.AddMessageContract<SubmitOrder>("orders.submit", majorVersion: 1);
```

This is an excerpt inside reliable configuration. Separate feature/bus declarations contribute to one application-wide catalog, which is built once. The builder rejects a type with conflicting identities, an identity assigned to another type and invalid/open-generic contracts.

Stable durable identity does not automatically migrate old envelope identifiers or entity names after a CLR rename. Preserve all relevant compatibility boundaries.

## Body versus envelope

The body contains application data. The envelope adds message types, IDs, source/destination/reply information, headers and other infrastructure metadata.

The envelope JSON serializer uses `application/vnd.vicione.servicebus+json`. It deserializes metadata and creates a context that can materialize matching contracts. Raw JSON uses a different serializer/context path; it lacks the same envelope and therefore requires deliberate message-type/header interoperability settings.

MessagePack is an optional binary format. Sender and receiver must agree on supported format and contracts. Adding the package does not automatically make every foreign message acceptable.

## Immutable configuration and bytes

The JSON serializer requires an immutable options snapshot. Changing serialization options after endpoint construction would otherwise create inconsistent behavior between operations.

At send time, exact-byte admission attaches to the transport context. It rejects serialization before admission attachment, mismatched content type, wrong-bus ownership and incomplete admitted body/envelope evidence. Reliable capture preserves serialized bytes and metadata for replay rather than relying on a mutable application object.

A serializer extension must cooperate with these contracts. Returning bytes of the correct general format is insufficient if it cannot establish complete admission.

## Mandatory limits

Every bus declares body bytes, envelope bytes and JSON depth:

```csharp
bus.Limits(new MessageLimits
{
    MaxBodyBytes = 256 * 1024,
    MaxEnvelopeBytes = 512 * 1024,
    MaxJsonDepth = 32,
    WarnAboveBytes = 128 * 1024
});
```

This is a policy example, not a universal recommended limit. Validate it against your carrier and actual messages. Envelope maximum must be at least body maximum; positive declared values and optional thresholds are validated.

The Conservative policy uses 1 MiB, 2 MiB and depth 32. Those values are not a broker capability promise.

Incoming deserialization checks envelope length before materializing a consumer message. Malformed content and oversized payload fail before normal business handling.

## MessageData offload

Offload thresholds require a configured owner and evidence that a stored reference was produced. They do not magically externalize every arbitrary large object.

Design the contract with `MessageData<T>`, configure a compatible repository and consume transformation, and keep its value accessible throughout retries/subscriber lifetimes. The reference envelope still needs to fit transport limits.

See [Persistence and MessageData](../integrations/persistence-and-message-data.md).

## Evolution and durable replay

Adding compatible fields may be possible with the configured serializer/contract design, but compatibility is not guaranteed merely because a property is optional in one language. Readers, validation, polymorphism, topology and old retained records must be considered.

A breaking stable contract revision needs an explicit major-version decision. Removing a catalog mapping can make retained intents undispatchable and lead to quarantine. Old wire messages can remain in broker queues after deploying new consumers.

Coordinate sender, receiver and stored-data upgrades. Do not overwrite a mapping so the same stable identity silently changes meaning.

## Security and resource consequences

Serialized input is untrusted. Limit depth and size; validate business meaning after deserialization. Avoid exposing secrets in headers or exception/journal payloads. Explicit journal projections are safer than retaining every raw body.

Serialization and type compatibility are transport boundaries, not authorization. A valid `SubmitOrder` message still needs application checks.

Source: [identity](../../src/ViciOne.ServiceBus.Abstractions/Contracts/MessageContractIdentity.cs), [catalog builder](../../src/ViciOne.ServiceBus/Advanced/MessageContractCatalogBuilder.cs), [JSON serializer](../../src/ViciOne.ServiceBus/Serialization/Serializers/SystemTextJsonMessageSerializer.cs), [limits](../../src/ViciOne.ServiceBus.Abstractions/MessageLimits.cs) and [admission](../../src/ViciOne.ServiceBus/Serialization/Admission/PayloadAdmissionTransportBoundary.cs).
