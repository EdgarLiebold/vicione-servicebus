---
type: guide
title: "Consumers and dependency injection scopes"
description: "Explain consumer activation, ConsumeContext, scoped outgoing operations and async completion."
tags: [servicebus, messaging]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-f7b0bd8fd5ab46f53dce54ae
    resource: repo://src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableInboxContext.cs
  - id: openwiki-source-af370a5e97239ee356f9c45c
    resource: repo://src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableInboxContextFactory.cs
  - id: openwiki-source-a784efd13db311ce669d4825
    resource: repo://src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs
  - id: openwiki-source-8b8aa18afda93781057e753d
    resource: repo://src/ViciOne.ServiceBus/DependencyInjection/ConsumeScopeProvider.cs
  - id: openwiki-source-826ef62f11daf8fdbd267494
    resource: repo://src/ViciOne.ServiceBus/DependencyInjection/ScopeConsumerFactory.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Consumers and dependency injection scopes

A consumer is a typed message handler activated by a configured receive pipeline. Its dependency-injection scope carries the resources and outgoing messaging context for that invocation. This lifetime boundary is essential to understanding retries, transactions and completion.

## The minimal contract

```csharp
public sealed record SubmitOrder(Guid OrderId);

public sealed class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
    {
        Console.WriteLine(context.Message.OrderId);
        return Task.CompletedTask;
    }
}
```

This is a class/contract example, not a complete service. Register it with `AddConsumer<SubmitOrderConsumer>()` and configure the owning transport's receive endpoints. A class declaration alone does not receive anything.

A consumer can implement several typed consumer interfaces. Matching contracts and endpoint configuration determine invocation; unrelated message types do not become compatible because their property names resemble each other.

## Activation and scope ownership

The scope provider uses an existing appropriate scope or creates an owned scope. It resolves the consumer from that scope. The consumer factory awaits the pipeline and releases owned resources afterward.

Inject ordinary scoped business dependencies such as an application service or DbContext through the consumer constructor. Avoid retaining those dependencies in a singleton, or retaining a consume context beyond its delivery.

Scope reuse/creation also depends on pipeline placement. Adding a retry outside or inside the scope boundary changes which activation repeats. Inspect the configured behavior rather than assuming every retry always creates a brand-new consumer.

## What the context contains

`ConsumeContext<T>` combines `Message` with message ID, correlation/request/conversation information, headers, addresses, cancellation and response/outgoing operations. It also exposes underlying receive/serializer contexts for deliberate infrastructure use.

The receive input address identifies the endpoint accepting this delivery. The envelope destination can identify a publish entity instead. These addresses answer different routing questions.

The context's cancellation token belongs to the delivery. A consumer unexpectedly canceling its own operation while that token is active is classified separately from host/delivery cancellation.

## Await all owned business work

The runtime awaits `ConsumeAsync`. It also tracks registered consume tasks through `ConsumeCompleted`. Return the real task or await it; do not launch detached work and return a completed task.

An illustrative consumer response is:

```csharp
public Task ConsumeAsync(ConsumeContext<GetOrder> context) =>
    context.RespondAsync(
        new OrderStatus(context.Message.OrderId, "accepted"));
```

Its contracts and state lookup must be provided by the application. The response task follows the active endpoint policy; if the consumer has a transactional outbox, a captured response and a delivered response are separate observations.

## Outgoing messages belong to the receive scope

Use `context.Outgoing` for application follow-up messages. The surface offers send, publish and scheduling; wrappers bind it to the active consume policy and causal metadata.

```csharp
await context.Outgoing.PublishAsync(
    new OrderSubmitted(context.Message.OrderId),
    context.CancellationToken);
```

This excerpt assumes the appropriate typed context and event. The outgoing operation can dispatch directly, capture volatile work, or stage a persistent intent. Its successful task does not universally establish carrier delivery.

Scoped injected send/publish services are selected by the scoped bus context provider. A separately captured global bus reference can bypass the intended transactional/consume scope. See [Outbox](../reliability/transactions-and-outbox.md).

## Responses and deferred responses

`RespondAsync` uses the request's response routing and tracks the operation. `DeferResponse` expresses a response tied to successful consume completion; its implementation participates in the context's completion machinery.

Do not send “success” before the business boundary you mean to acknowledge. A process that answers a request before committing its database may expose a result that rollback invalidates.

A client waiting inside the same consumer for an outgoing operation whose release depends on that consumer finishing can deadlock conceptually. This is particularly important with volatile outgoing capture.

## Database effects

An ordinary DI scope is not a transaction. Injecting a DbContext does not automatically join consumer effects, outgoing messages and carrier settlement.

The unified EF inbox integration creates the supported transaction, wraps the consume context and uses its scoped outbox to save business state, consumed identity and outgoing intents together. The classic EF path has different state and configuration; do not treat it as the same implementation.

An external HTTP/payment call remains outside that transaction. Use business idempotency and explicit reconciliation where the effect cannot be atomically included.

## Definitions and component policy

Consumer definitions configure endpoint names and consumer behavior. Shared endpoint transport QoS belongs at the endpoint boundary, while consumer-specific retry/concurrency belongs to its appropriate stage. Sagas, activities, futures and jobs use specialized component activation/context rather than ordinary consumer state alone.

## Failure and cleanup

The consumer filter reports success or fault after awaited execution. Fault-observer or scope-disposal failure can add another cause; cleanup must not be ignored. The carrier then follows its receive/settlement policy.

Use [Failures](../reliability/failures-and-retries.md) for retry placement and [Lifecycle](../architecture/message-lifecycle.md) for the complete boundary chain.

Source: [ConsumeContext](../../src/ViciOne.ServiceBus.Abstractions/ConsumeContext.cs), [scope provider](../../src/ViciOne.ServiceBus/DependencyInjection/ConsumeScopeProvider.cs), [scope factory](../../src/ViciOne.ServiceBus/DependencyInjection/ScopeConsumerFactory.cs) and [consumer filter](../../src/ViciOne.ServiceBus/Middleware/ConsumerMessageFilter.cs).
