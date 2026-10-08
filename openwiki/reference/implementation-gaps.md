---
type: guide
title: "Known implementation and concept discrepancies"
description: "Explain source-confirmed reliability discrepancies and direct readers to the separately maintained inconsistency register without treating missing implementations as supported guarantees."
tags: [servicebus, reference]
verified:
  - by: openwiki/0.7.1
    at: 2026-10-07T17:13:31.418Z
sources:
  - id: openwiki-source-c7201847f00699d4bb6682c1
    resource: repo://src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ReliableMessaging/ReliableRecurringScheduleRecord.cs
  - id: openwiki-source-906a5e75db9d003121c0b565
    resource: repo://src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs
  - id: openwiki-source-7b6f33fdb49050f8a70b8039
    resource: repo://src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingPolicy.cs
  - id: openwiki-source-92706c6b67cedb2cdfd7da0d
    resource: repo://src/ViciOne.ServiceBus/Scheduling/MessageScheduler.cs
generated: { by: "codex", at: "2026-10-07T17:13:31.418Z" }
---

# Known implementation and concept discrepancies

<!-- openwiki: broken internal link [</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>] file "</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>" does not exist. Fix the href or restore the target, then delete this comment. -->
This chapter records discrepancies found while tracing the current system. It is part of the explanatory wiki so readers do not infer a guarantee from an unfinished integration. The separate [working inconsistency register](</Users/edgar.liebold/Library/CloudStorage/Dropbox/_Temp/OpenWiki/servicebus-inconsistencies.md>) contains the detailed evidence, consequences and required dispositions.

The register is an ongoing source review, not a completed architecture audit. None of these entries implies that product code has been fixed. Differences between transport, store and workflow responsibilities are intentional when their contracts are explicit; the findings below concern contradictory concepts or disconnected implementation.

## SB-ARCH-01: parallel EF transactional models

The reliability documentation presents a unified store for outgoing intents, inbox state and scheduling. That path uses `EntityFrameworkReliableStore`, `DurableSendRecord` and `ReliableInboxRecord`.

Separate public configuration remains available through `ConfigureEntityFrameworkTransactionalStore`, `EnableTransactionalOutbox` and `UseEntityFrameworkOutbox`. It uses `InboxState`, `OutboxState` and `OutboxMessage`, with separate policies and operations.

Both paths feed the same reliable delivery coordinator. The inconsistency is not two independent workers: it is two selectable record and guarantee models under a documented unified concept. The separate path replays through ordinary endpoint sends, whereas unified delivery consumes explicit dispatcher acceptance/completion results. An architectural decision must establish whether both models remain supported and how their guarantees differ.

## SB-ARCH-02: stored recurrence is disconnected

The EF reliable model maps `ReliableRecurringScheduleRecord`, and documentation describes reliable-store recurring schedules. Source tracing finds no connected creation, advancement or pause/resume runtime for those records. The schedule store contract and reliable scheduler implement one-time scheduled delivery.

Quartz recurrence and recurring jobs have separate implementations. Their existence does not implement the mapped reliable-store recurrence model. Use their actual adapters and lifecycle contracts instead of assuming a mapped table is a working scheduler.

## SB-ARCH-03: terminal retention is configuration without cleanup

Reliable configuration requires a positive `Retention` value and freezes it into policy. The public description associates it with retained terminal state. No runtime cleanup consumer of that policy was found.

The separate EF model's duplicate-detection cleanup and journal retention belong to other mechanisms. They cannot be used as evidence that unified inbox terminal state is automatically pruned. Database lifecycle and replay-safety decisions remain necessary; configured retention alone does not establish enforcement.

## SB-ARCH-04: capacity scope is narrower than its description

The store-limit contract describes shared limits across outgoing work, inbox/quarantine and schedules. The current capacity ledger reserves and releases outgoing records. Inbox acquisition and insertion do not reserve that ledger; the in-memory inbox is also held separately.

The outgoing snapshot and health check consequently cannot establish a total bound on every reliable-store table. Together with unimplemented terminal retention, this leaves an important storage-growth gap. See [inbox](../reliability/inbox-and-idempotency.md) and [health](../operations/telemetry-and-health.md) for the practical boundaries.

## SB-ARCH-05: scheduled publication uses command routing

The general scheduler resolves a publication through bus publish topology. The reliable scheduler's `SchedulePublishAsync` resolves a configured send route and calls scheduled send instead.

A contract can have publish topology without a configured send route. A mapped route can also target one queue rather than the event's subscriber topology. Thus selecting a scheduler changes the prerequisites and meaning of the same public operation. Provider fan-out runtime cases still need verification, but the different source resolution paths are confirmed.

## How to use these findings

For a deployment decision, read the affected mechanism chapter and select only guarantees actually supplied by its implementation. For a product correction, use the detailed register's evidence and validation requirements. Do not conceal these entries behind the phrase “older integrations”: public APIs that remain selectable require an explicit support or migration disposition.

## Source grounding

<!-- openwiki: broken internal link [../../src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore] file "../../src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore" does not exist. Fix the href or restore the target, then delete this comment. -->
The [reliability description](../../docs/reliability.md), [EF provider](../../src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore), [reliable policy](../../src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessagingPolicy.cs), and [reliable scheduler](../../src/ViciOne.ServiceBus/Configuration/ReliableMessaging/ReliableMessageScheduler.cs) ground these findings.
