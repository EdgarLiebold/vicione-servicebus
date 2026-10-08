# ViciOne.ServiceBus inconsistencies

This is the working inconsistency register maintained during the English wiki rebuild. It records discrepancies found in the current source, public API, documentation, and interactions between system components. It is separate from generated wiki content so regeneration cannot silently erase findings.

Repository: `/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus`  
Started: 2026-10-07  
Review status: ongoing; this is not a completed architecture audit. No product fixes or new runtime verification are implied by this register.

## Classification

- **Confirmed inconsistency:** the cited evidence establishes contradictory concepts, contracts, or behavior.
- **Open question:** a possible inconsistency still needs source tracing, runtime evidence, or an architectural decision.
- **Resolved:** the discrepancy has an explicit disposition or a verified correction; its history remains here.

Different mechanisms are not automatically inconsistent. The register distinguishes an intentional difference in responsibility from conflicting ownership or guarantees.

## Summary

| ID | Finding | Classification | Status |
|---|---|---|---|
| SB-ARCH-01 | Two public EF inbox/outbox models remain alongside the stated unified reliability model | Confirmed inconsistency between documentation and implementation | Open; architectural disposition required |
| SB-ARCH-02 | Reliable-store recurring scheduling is described but has no connected implementation | Confirmed inconsistency between documentation and implementation | Open |
| SB-ARCH-03 | Required terminal-state retention policy has no runtime consumer | Confirmed inconsistency between public contract and implementation | Open |
| SB-ARCH-04 | Shared store bounds are described more broadly than their enforced outbox scope | Confirmed inconsistency between public contract and implementation | Open |
| SB-ARCH-05 | Reliable scheduled publication resolves an addressed send route instead of publish topology | Confirmed semantic inconsistency between scheduler implementations | Open; runtime provider scenarios need verification |

## SB-ARCH-01 — Parallel public EF inbox/outbox models

### Expected concept

The reliability documentation describes one model for durable send, transactional outgoing messages, duplicate-safe consumption, scheduling, retry, and quarantine. The migration guide directs applications from previous outbox configurations to `UseReliableMessaging(...)` with one selected store.

### Observed implementation

The unified path, `UseReliableMessaging(...UseEntityFramework<TDbContext>())`, registers `EntityFrameworkReliableStore<TBus,TDbContext>` as the outbox, inbox, and schedule store. Its transactional scoped context stages `DurableSendRecord` entries; its automatic receive integration uses `ReliableInboxRecord`.

A separate public path remains available through `ConfigureEntityFrameworkTransactionalStore<TDbContext>(...)`, `EnableTransactionalOutbox(...)`, and `UseEntityFrameworkOutbox<TDbContext>(...)`. It uses `InboxState`, `OutboxState`, and `OutboxMessage`, mapped through `AddTransactionalOutboxEntities(...)`, with separately configured query, locking, delivery, and duplicate-detection policies.

Both paths feed the same `ReliableMessagingDeliveryService<TBus>`: the separate transactional model registers an `IReliableDeliverySource<TBus>`. This finding therefore does **not** establish two independent hosted delivery coordinators for one bus.

Their delivery contracts differ. The separate transactional delivery source replays through an ordinary endpoint's `SendAsync`. The unified store path uses `IDurableSendDispatcher<TBus>` and handles its explicit `TransportAcceptance` or `ConsumerCompletion` result. Their operational surfaces also differ: `IEntityFrameworkOutboxOperations<TBus,TDbContext>` versus `IReliableMessagingOperations<TBus>`.

The separate configuration APIs appear in the committed packed public API baseline. They are publicly selectable product surface, not merely unused internal names.

### Consequences

Applications can select two persisted inbox/outbox models despite documentation presenting one unified model. A common background coordinator does not unify their stored records, acceptance boundaries, or operational queries. Documentation must not imply that guarantees or quarantine visibility automatically transfer between them.

Whether both paths should remain is an architectural decision. A runtime defect from combining them has **not** been established, and this finding does not claim that two databases are always used. The confirmed discrepancy is between the stated unified concept and the selectable implementation models.

### Evidence

All links below target the current local repository.

- [Reliability concept](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/docs/reliability.md>) and [migration guide](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/docs/changelog/migration-from-masstransit.md>).
- [Separate public configuration and mappings](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Configuration/EntityFrameworkOutboxConfigurationExtensions.cs:19>).
- [Separate configuration policy](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Configuration/EntityFrameworkOutboxConfigurator.cs>).
- [Separate scoped context, operations, and shared worker registration](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Configuration/EntityFrameworkBusOutboxConfigurator.cs:76>).
- [Unified EF registration](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableMessagingServiceCollectionExtensions.cs>).
- [Unified record staging](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkScopedBusContext.cs:195>) and [separate transactional context](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkTransactionalScopedBusContext.cs>).
- [Separate ordinary-endpoint replay](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkTransactionalOutboxSource.cs:336>) and [unified dispatcher outcome handling](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableMessagingDeliveryService.Delivery.cs>).
- [Shared delivery coordinator](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableMessagingDeliveryService.cs>).
- [Packed public API baseline](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/docs/api/packed-public-api.txt:7643>).

### Required disposition

Consolidate publicly supported persisted reliability into the stated unified model, or explicitly define the purpose of each retained model, its guarantees, supported combinations, and operational scope. The wiki may explain current behavior; it must not present the unresolved divergence as architecture approval.

### Validation and resolution

Source tracing and comparison with the public API baseline completed on 2026-10-07. No new runtime tests were executed for this finding. Status remains open.

## SB-ARCH-02 — Reliable-store recurring scheduling is described but not connected

### Expected concept

`docs/reliability.md` says recurring schedules create due outbox records from the same application-owned store. `docs/migrations/README.md` includes recurring schedules among state stored by the EF reliable provider.

### Observed implementation

`ReliableRecurringScheduleRecord` exists and `AddViciOneReliableMessaging()` maps its table. A search across all product C# sources finds references to this type only in its declaration and model mapping. No product path creates, reads, advances, pauses, or dispatches these records.

`IScheduleStore<TBus>` exposes single-intent scheduling and cancellation. The stored scheduler registers `IMessageScheduler`, not a store-backed `IRecurringMessageScheduler`. Recurring scheduler registration occurs for the endpoint adapter, with scheduler commands handled by integrations such as Quartz.

### Consequences and limits

A mapped recurring-schedule table does not establish working store-backed recurring delivery. The wiki must distinguish working one-time due intents and adapter-backed recurring scheduling from this unconnected schema. This finding does **not** claim that Quartz recurring jobs or JobService's own recurring state machine are absent.

### Evidence

Repository-relative source anchors:

- `docs/reliability.md`, opening data-flow explanation.
- `docs/migrations/README.md`, opening schema description.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/ReliableRecurringScheduleRecord.cs`.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableMessagingModelExtensions.cs`, recurring model mapping.
- `src/ViciOne.ServiceBus.Abstractions/Providers/Persistence/ReliableMessaging/IScheduleStore.cs`, complete scheduling SPI.
- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs`.
- `src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableSchedulerRegistration.cs`, stored versus endpoint registrations.
- `src/Scheduling/ViciOne.ServiceBus.Quartz/QuartzSchedulingExtensions.cs`, recurring scheduler command consumers.

Verification: repository-wide references to `ReliableRecurringScheduleRecord` and registration/use of `IRecurringMessageScheduler`, followed by inspection of the store and scheduler contracts. No new runtime tests executed.

### Required disposition

Implement and verify the claimed store-backed recurring lifecycle, or explicitly remove/defer that capability claim and classify the unused mapping. Define how adapter-backed recurrence relates to the one-store reliability concept.

## SB-ARCH-03 — Required retention policy has no runtime consumer

### Expected contract

`IReliableMessagingConfigurator.Retention(duration)` and `ReliableMessagingOptions<TBus>.Retention` describe a retention duration for terminal inbox and recurring-schedule state. Configuration must declare a positive duration; missing or invalid retention is rejected.

### Observed implementation

The configurator stores the value, options validation checks it, and `ValidateAndFreeze()` copies it into `ReliableMessagingPolicy<TBus>`. A product-wide search for `Retention` finds no runtime use of this reliable-messaging policy value beyond configuration and freezing.

The unified EF and InMemory stores retain terminal inbox state without a connected cleanup policy consuming that duration. The independently retained classic EF integration has its own `DuplicateDetectionWindow` and `InboxCleanupService`; those do not establish enforcement of unified `Retention`.

### Consequences and limits

A required, validated setting suggests an operational effect that the current unified runtime does not implement. Applications cannot rely on setting `Retention(TimeSpan.FromDays(7))` to prune terminal reliable inbox records after seven days. Indefinite retained state affects database growth and the duplicate-detection horizon.

This is a source-level missing connection. External application cleanup could exist outside this repository; it would not make the product configuration enforce its declared policy. Journal retention and MessageData lifetimes are separate contracts and are not implicated by this finding.

### Evidence

- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/IReliableMessagingConfigurator.cs`, retention contract.
- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingConfigurator.cs`, retention assignment.
- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingOptions.cs`, required validation and freeze.
- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingPolicy.cs`, retained policy field.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableStore.cs`, inbox completion and operational transitions.
- `src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/InMemory/InMemoryReliableStore.Inbox.cs`.
- `src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/ReliableMessagingDeliveryService.cs`, connected worker lifecycle.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Configuration/EntityFrameworkOutboxConfigurator.cs`, separate classic cleanup registration.

Verification: product-wide retention references plus inspection of unified store and worker paths. No new runtime tests executed.

### Required disposition

Connect an explicitly owned cleanup lifecycle to the retention policy and define its duplicate-safety consequences, or revise the public contract to match actual behavior. Do not silently prescribe deleting inbox records in the wiki.

## SB-ARCH-04 — Advertised shared capacity does not bound retained inbox state

### Expected contract

`ReliableStoreLimits` describes hard retained-storage limits shared by outbox, inbox quarantine, and schedules. `ReliableMessagingOptions<TBus>.MaximumStoredCount` describes a bound for all retained reliable-messaging records.

### Observed implementation

The EF capacity ledger is incremented by outgoing intent admission and transactional outgoing capture and decremented when those outgoing records are retired or removed. Inbox acquisition and transactional inbox processing add `ReliableInboxRecord` rows without reserving that capacity. The InMemory implementation likewise maintains its inbox dictionary separately from outgoing record count and content bounds.

The reliable health check reads the outgoing store snapshot. That snapshot does not establish an aggregate count or byte bound over retained inbox records. Scheduled one-time outgoing intents do participate in outgoing admission limits; the issue is the advertised broader scope.

### Consequences and limits

Configuring `MaximumStoredCount = 10_000` does not imply that at most 10,000 total outgoing and inbox records are retained. A service can consume many unique messages while its outgoing capacity snapshot stays below the bound. Together with SB-ARCH-03, retained inbox state has neither the advertised shared capacity enforcement nor the configured terminal cleanup connection.

This finding concerns product-level logical bounds. It does not claim that the application database has no physical quota, or that every store implementation outside this repository behaves the same way.

### Evidence

- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableStoreLimits.cs`, declared shared scope.
- `src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingOptions.cs`, maximum-record contract.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableStore.cs`, admission capacity ledger versus `AcquireAsync` and inbox transitions.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/EntityFrameworkReliableInboxContextFactory.cs`, transactional inbox insertion.
- `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkScopedBusContext.cs`, outgoing capacity reservation.
- `src/ViciOne.ServiceBus/Providers/Persistence/ReliableMessaging/InMemory/InMemoryReliableStore.cs` and `InMemoryReliableStore.Inbox.cs`, separate incoming record storage.
- `src/ViciOne.ServiceBus/Operations/ReliableMessaging/DurableSenderHealthCheck.cs`, outgoing snapshot-based health.

Verification: capacity mutation sites and inbox admission/processing source tracing. No new runtime tests executed.

### Required disposition

Define and enforce the intended capacity domain, including incoming and terminal records if the shared bound is required. Otherwise name and document the bounds as outgoing-intent limits and specify separately owned inbox growth controls.

## SB-ARCH-05 — Reliable scheduled publication uses a send route

### Expected concept

`IMessageScheduler.SchedulePublishAsync` promises scheduling for publication. Ordinary publication resolves topology for a contract so subscribers can receive the event. The general `MessageScheduler` implements scheduled publication by calling `GetPublishAddress<T>()` through `IBusTopology.TryGetPublishAddress<T>`.

### Observed implementation

`ReliableMessageScheduler<TBus>.SchedulePublishAsync` instead calls `EndpointConvention.GetDestinationAddress<T>(_bus)` and forwards to `ScheduleSendAsync`. That convention consults the owning bus's configured message route table and throws if no route exists. It does not resolve publish topology.

An explicitly mapped route could point at a publish exchange, but the method does not establish that mapping or preserve publication semantics independently of it. A route mapped to a consumer queue schedules addressed delivery, while subscribers on other queues are outside that route.

### Consequences

Selecting the reliable scheduler changes the meaning and prerequisites of the same scheduling API. An event that can be published through normal topology can fail for a missing send route, or can be scheduled to one configured destination instead of its publish topology. The source establishes the semantic mismatch; no new RabbitMQ or in-memory fan-out runtime test has been executed for this finding.

### Evidence

- [Public scheduling contract](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus.Abstractions/Scheduling/IMessageScheduler.cs>).
- [Reliable scheduler](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs:111>).
- [General scheduler](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus/Scheduling/MessageScheduler.cs:193>).
- [Send route resolution](</Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/src/ViciOne.ServiceBus/Advanced/EndpointConvention.cs:39>).

### Required disposition

Resolve publication through the owning bus's supported publish topology and reconcile it with durable-dispatch destination restrictions, or expose/document addressed scheduled delivery as a different operation. Do not silently substitute command routing for event publication.

### Validation needed

Compare scheduled publication with ordinary publication for a contract with two independently bound subscribers and no send-route mapping. Repeat with a mapped consumer-queue route. Verify supported reliable carriers, admission, and failures without asserting that all providers can durably target arbitrary publish topology.
