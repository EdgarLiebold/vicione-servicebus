---
type: guide
title: "Sending commands and publishing events"
description: "Explain practical routing, context propagation, options and failure boundaries."
tags: [servicebus, messaging]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-ff185132eefab169907ca281
    resource: repo://docs/api/completion-boundaries.md
  - id: openwiki-source-a94e9fdab69dcc6c95b05e53
    resource: repo://src/ViciOne.ServiceBus.Abstractions/IOutgoingMessages.cs
  - id: openwiki-source-cf0e300f0dde132d51fb8986
    resource: repo://src/ViciOne.ServiceBus.Abstractions/SendOptions.cs
  - id: openwiki-source-214a678ecc498b7aa55942fe
    resource: repo://src/ViciOne.ServiceBus/Transports/Sending/SendEndpoint.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Sending commands and publishing events

Send and publish express different routing intent. Send addresses work to a destination. Publish sends a message through type-based topology to subscribed destinations. Choose based on business ownership before considering convenience overloads.

## Addressed commands

A command normally has one logical owner, although several receiver instances may compete on that owner's queue.

```csharp
ISendEndpoint endpoint = await sender.GetSendEndpointAsync(
    destination, cancellationToken);

await endpoint.SendAsync(
    new SubmitOrder(orderId, customerId),
    cancellationToken);
```

This excerpt assumes `ISendEndpointProvider sender`, an absolute destination and compatible contract/consumer. Endpoint resolution can normalize a transport-relative address, acquire cached transport resources and fail before the actual send.

A successful direct send means the active transport operation completed. It does not by itself prove consumption or a business commit.

## Published events

```csharp
await publisher.PublishAsync(
    new OrderSubmitted(orderId),
    cancellationToken);
```

Here `publisher` is `IPublishEndpoint`. The message-type topology determines destinations. Independent subscriber queues receive independent copies; processes sharing one queue compete for that copy.

Publish has no universal historical replay guarantee for a subscriber created later. Durable subscriptions, queue lifetime and broker retention are provider configuration. The application chooses whether a domain event needs a recovery/replay mechanism beyond the bus.

## Metadata options

Typed options add application headers and causal identity without requiring a custom send pipe:

```csharp
await endpoint.SendAsync(
    command,
    new SendOptions
    {
        MessageId = messageId,
        CorrelationId = orderId,
        TimeToLive = TimeSpan.FromMinutes(5)
    },
    cancellationToken);
```

Use unique message identity for an individual operation and deliberate correlation for related business work. TTL limits eligibility; it is not a receiver execution timeout. A policy may discard expired work without establishing delivery.

Provider-native routing/partition features require compatible selected transport behavior. A partition key in options does not create a global ordering guarantee for every carrier.

## From a consumer

Prefer the receive-scoped outgoing surface:

```csharp
await context.Outgoing.PublishAsync(
    new OrderSubmitted(context.Message.OrderId),
    context.CancellationToken);
```

It preserves the active consume context and configured outgoing policy. Scoped send/publish services can likewise resolve through bus-qualified context. A raw singleton bus captured elsewhere can select a different boundary.

Use a configured route for `Outgoing.SendAsync(message)`, or the explicit destination/options overload when necessary. Missing or ambiguous routes are configuration failures rather than permission to silently publish instead.

## What awaiting actually means

| Active policy | Send/publish task can complete after |
|---|---|
| Direct endpoint | Selected transport operation |
| Buffered bus | Local queue admission |
| Volatile outbox | Consume-scope capture |
| Transactional EF outbox | Serialization and DbContext staging |

For persistent capture, commit is separate; later delivery is separate again. For buffered work, explicitly flush. For volatile capture, successful consumer completion releases outgoing actions.

Consequently, this code can be wrong even though both tasks were awaited: update a database directly, then publish, with no shared transaction. If the publish fails after the database commit, the application loses its event unless it recorded an intent.

Read [Outbox](../reliability/transactions-and-outbox.md) for that dual-write problem.

## Exceptions and cancellation

Serialization/limits, endpoint resolution, topology declaration and provider send are distinct failure stages. Some fail before dispatch; others leave acceptance uncertain. Caller cancellation does not revoke a message already accepted by the carrier.

Avoid blind retry of an effectful command based solely on a timeout. Use receiver idempotency and a stable logical operation identity where necessary. Durable sender admission also has its own retained identity semantics; it is not an alias for every ordinary send.

## Publishing and request routing

A request can be addressed or use publish routing; response correlation still needs request identity and a reply destination. Publishing a query to several independent owners can create competing responses and ambiguous business semantics. Use [Request/response](request-and-response.md) to make the result owner explicit.

## Naming and evolution

Type-based topology and endpoint naming are deployment-facing choices. Renaming a contract or queue can leave old messages/subscriptions behind. Verify intended destinations and compatible readers before changing them.

For a runtime trace see [Message lifecycle](../architecture/message-lifecycle.md). For physical routing see [Endpoints](../configuration/endpoints-and-topology.md).

Source: [send endpoint](../../src/ViciOne.ServiceBus/Transports/Sending/SendEndpoint.cs), [send options](../../src/ViciOne.ServiceBus.Abstractions/SendOptions.cs), [outgoing contract](../../src/ViciOne.ServiceBus.Abstractions/IOutgoingMessages.cs) and the [send](../../samples/DeveloperJourneys/Journey02Send.cs)/[publish](../../samples/DeveloperJourneys/Journey03Publish.cs) journeys.
