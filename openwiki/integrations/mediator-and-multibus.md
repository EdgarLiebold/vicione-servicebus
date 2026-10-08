---
type: guide
title: "Mediator and multiple buses"
description: "Explain process-local dispatch and bus identity/scoped ownership with examples."
tags: [servicebus, integrations]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-5dccae0cde5bdef1c823fe1d
    resource: repo://src/ViciOne.ServiceBus.Mediator/Mediator/Runtime/InProcessMediator.cs
  - id: openwiki-source-296c55b271766c5db0f54a73
    resource: repo://src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs
  - id: openwiki-source-d7ea19bd7705e88aadf9dba0
    resource: repo://src/ViciOne.ServiceBus/Configuration/DependencyInjection/ServiceCollectionBusConfigurator.cs
  - id: openwiki-source-137a877940f14c2104bab427
    resource: repo://src/ViciOne.ServiceBus/DependencyInjection/ScopedBusContextProvider.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Mediator and multiple buses

Mediator and MultiBus address different boundaries. Mediator changes where dispatch happens: inside one process. MultiBus changes which configured runtime owns an operation: one of several typed buses. They can share dependency injection infrastructure without sharing persistence or delivery guarantees.

## Mediator's execution model

`InProcessMediator` owns a primary receive dispatcher and a response dispatcher. Its endpoint dispatches send/publish work directly through receive pipelines, while a client factory correlates responses. There is no broker hop.

This retains consumer activation, configured middleware, message limits, observers and request timing. It is useful when you want messaging-style decoupling between local components or want to exercise consumer behavior without an external carrier.

It does not persist an in-flight operation across process loss. A mediator request and a broker request therefore differ operationally even when the consumer contract looks similar.

## Send, publish and request locally

A local send dispatches through the mediator's configured consumers. Publish follows the mediator's matching dispatch behavior rather than creating broker subscriptions. Request connects a pending response through its response dispatcher.

The returned task belongs to this direct dispatch model. Do not transfer assumptions from buffered or broker endpoint completion to it. Cancellation can stop waiting or cooperative execution; it cannot undo external work already performed by a local consumer.

The mediator uses its own time source and limits. It does not inherit every bus feature merely because a bus is registered in the same provider. Use its registration and configuration surface explicitly.

See [Consumers](../messaging/consumers-and-scopes.md), [Requests](../messaging/request-and-response.md) and the [mediator registration source](../../src/ViciOne.ServiceBus.Mediator/DependencyInjection/MediatorServiceCollectionExtensions.cs) for current composition shapes.

## Why an application might use more than one bus

A service may need separate carrier connections, identities or operational ownership—for example, orders and billing isolated by configuration. Typed interfaces disambiguate those runtimes.

Configuration excerpt from the developer journey:

```csharp
public interface IOrdersBus : IBus;
public interface IBillingBus : IBus;

services
    .AddViciOneServiceBus<IOrdersBus>("orders-v1", orders =>
    {
        orders.Limits(MessageLimits.Conservative);
        orders.UsingInMemory();
    })
    .AddViciOneServiceBus<IBillingBus>("billing-v1", billing =>
    {
        billing.Limits(MessageLimits.Conservative);
        billing.UsingInMemory();
    });
```

The two interfaces identify runtime owners. The strings are stable persistence identities. This excerpt configures two local buses without consumers or persistence; additional capabilities belong inside the appropriate block.

## Stable identity versus CLR type

The registration key identifies the configured bus contract in the container. Persistent features additionally need an explicit identity that survives CLR type renaming.

Keeping retained records while changing the identity can orphan state; accidentally reusing an identity can mix ownership. Choose and maintain identity as a deployment contract, not a cosmetic label.

The default bus has its default persistence identity. Typed registration without an explicit identity leaves persistent ownership unspecified until a feature requires it.

## Scoped services choose an owner

The registration graph binds scoped send/publish/client/context services to their bus. The ordinary scoped context provider chooses a matching consume context when one exists, a typed wrapper when entering another bus from a global consume scope, or a normal bus context outside consumption.

Typed `Bind<TBus,...>` services expose ownership-qualified infrastructure. Application code can also inject the typed bus for ordinary operations:

```csharp
await ordersBus.PublishAsync(new OrderSubmitted(orderId), cancellationToken);
```

This sends through orders, not whichever default bus happens to be present. The appropriate contract and instance must be supplied by the application.

## Crossing buses is not an atomic handoff

Consuming on one bus and publishing on another does not automatically share an inbox or outbox transaction. Metadata propagation provides causality, not atomicity. If a handoff must survive failure, design which application transaction records the outgoing intent and which bus/store owns it.

The EF scoped outbox selector also requires a deliberate default when multiple DbContext factories exist. The current parallel EF reliability models make those choices especially important; see [Implementation gaps](../reference/implementation-gaps.md).

## Shared provider, distinct observations

The depot starts all bus instances under the host. Each keeps transport ownership and endpoints. Within one service provider, telemetry uses the provider's meter factory scope; this does not combine state ownership or make all buses healthy when one succeeds.

Mediator cleanup disposes its client and observer resources. Bus cleanup stops carrier/endpoints through runtime lifecycle. Always dispose the owner you created rather than assuming one integration drains every other one.

Source: [mediator runtime](../../src/ViciOne.ServiceBus.Mediator/Mediator/Runtime/InProcessMediator.cs), [typed registration](../../src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs), [scoped bus selection](../../src/ViciOne.ServiceBus/DependencyInjection/ScopedBusContextProvider.cs) and [MultiBus example](../../samples/DeveloperJourneys/Journey09MultiBus.cs).
