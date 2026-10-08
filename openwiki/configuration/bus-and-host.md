---
type: guide
title: "Bus composition and host lifecycle"
description: "Explain DI registration, limits, transport ownership, endpoint configuration, startup readiness and shutdown."
tags: [servicebus, configuration]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-501f067e7f840aa1cbbeb75f
    resource: repo://src/ViciOne.ServiceBus/Configuration/BusCompositionValidation.cs
  - id: openwiki-source-296c55b271766c5db0f54a73
    resource: repo://src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs
  - id: openwiki-source-f779c9830be34e51707fd114
    resource: repo://src/ViciOne.ServiceBus/Hosting/ServiceBusHostedService.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Bus composition and host lifecycle

A bus configuration declares one runtime owner: its consumers, transport, endpoints, limits and optional capabilities. Hosting starts and stops that graph. A service collection full of registrations is not yet a running bus.

## Compose one bus deliberately

This configuration excerpt assumes an existing service collection and a defined consumer:

```csharp
services.AddViciOneServiceBus(bus =>
{
    bus.Limits(MessageLimits.Conservative);
    bus.AddConsumer<SubmitOrderConsumer>();
    bus.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host("localhost", credentials =>
        {
            credentials.Username("guest");
            credentials.Password("guest");
        });
        rabbit.ConfigureEndpoints(context);
    });
});
```

Use `Microsoft.Extensions.DependencyInjection`, `ViciOne.ServiceBus`, `ViciOne.ServiceBus.Configuration` and `ViciOne.ServiceBus.RabbitMq`. The [first application](../learn/first-application.md) supplies complete context.

Read the block in stages. Limits constrain this bus. Consumer registration defines component activation and policy. Transport selection provides a runtime factory. Endpoint configuration uses the registration context to build actual receiving endpoints.

## Registration does not imply receiving

`AddConsumer<T>` and `ConfigureEndpoints(context)` answer different questions. The former registers a component; the latter configures its endpoints on the selected carrier. A consumer existing in an assembly does not make it receive messages.

Manual endpoints allow explicit grouping and topology. Automatic endpoint configuration applies definitions and naming. Combining manual and automatic configuration requires checking which registrations are included, so a component does not accidentally appear on unintended endpoints.

The registration context also discovers sagas, activities and specialized kinds. Thus “configure endpoints” is a common composition operation, not only a loop over ordinary consumers.

## Startup validation establishes ownership

The startup validator requires exactly one transport and one limit owner for each bus. It reports missing and conflicting owners and attempts to materialize the bus graph before normal runtime startup.

Reliable messaging adds ownership checks for the shared contract catalog, outbox, inbox, schedule store and durable dispatcher. Its policies must explicitly configure positive capacity, delivery settings and retention. Unsupported dispatcher composition fails rather than silently substituting a weaker send.

These static checks do not prove that every provider option is universally validated at the same point, or that the broker/database is reachable. Current engineering work records remaining validation boundaries. A valid graph, a reachable carrier and a healthy backlog are separate conditions.

## Limits belong to the bus

`MessageLimits.Conservative` is 1 MiB body, 2 MiB envelope and JSON depth 32. The envelope includes metadata as well as the body. A broker's native limit can be lower, so choose a policy compatible with the selected carrier.

One policy must own the bus. Repeating equivalent declarations still creates ambiguous ownership. MessageData offload requires a configured repository and actual stored-reference evidence; declaring a threshold does not create blob storage.

## Host lifecycle

DI registration installs the composition validators before the shared `ServiceBusHostedService`. The hosted service resolves a depot grouping the registered bus instances. The depot starts their lifecycle controls; the runtime starts its host/endpoints and waits for readiness.

Startup may be observed in the background. If application startup must await bus readiness, set:

```csharp
services.Configure<ViciOneServiceBusHostOptions>(options =>
{
    options.WaitUntilStarted = true;
});
```

Without this choice, host startup alone does not mean the background start task succeeded. The hosted service observes and logs its failure.

Start and stop are serialized by a lifecycle gate. Stop coordinates with startup and delegates shutdown to the depot; async disposal also stops the service. Restarting the same hosted service after stopping has begun is rejected. Configured lifecycle timeouts use its time provider.

## Optional features and lifecycle owners

Keep bus-specific reliability and journaling in the owning bus block. A typed bus with retained state needs stable persistence identity. A Quartz adapter checks for a competing scheduler lifecycle owner rather than letting two hosted services manage the same scheduler.

Stateful components still need their own repository selections and schema. Registering Sagas or JobService does not deploy those tables. Database deployment belongs to the application.

The separate public EF transactional-store path is a known inconsistency with the unified concept; do not treat it as an interchangeable recipe without reading [Implementation gaps](../reference/implementation-gaps.md).

## MultiBus and shutdown

Typed bus contracts select distinct owners. Each requires its own transport and limits. Stable persistence identity keeps retained records associated with the intended bus across CLR type renames. Cross-bus publication does not create a distributed transaction.

During shutdown, await host stop and owned cleanup. Give business work a bounded cancellation policy; do not detach tasks to make shutdown appear fast. A killed process loses volatile state, while committed persistent intent depends on its store, not graceful shutdown.

## Common mistakes

| Symptom | Likely boundary to inspect |
|---|---|
| Startup configuration failure | Missing/duplicate limits, transport or feature owners |
| Consumer never invoked | Endpoint configuration and message topology |
| Host alive, broker unavailable | Background start task and readiness |
| Reliable dispatcher missing | Provider support and selected bus |
| Shutdown stalls | Active consumer/provider work and cancellation |
| Unexpected bus receives work | Typed bindings, registration context and addresses |

Source: [DI registration](../../src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs), [composition checks](../../src/ViciOne.ServiceBus/Configuration/BusCompositionValidation.cs), [hosted service](../../src/ViciOne.ServiceBus/Hosting/ServiceBusHostedService.cs) and [depot](../../src/ViciOne.ServiceBus/Transports/Hosting/BusDepot.cs).
