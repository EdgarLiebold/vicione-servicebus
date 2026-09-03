# V4 deterministic runtime-time ownership validation

Date: 2026-09-03

## Bound inputs and scope

- Package: reviewer integration 9/12, V4 checkpoint 008.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-08` at local architecture
  commit `8246d041`.
- Product baseline: `0a41b94737886b3cdfa01876ef021e15336564ee`, tree
  `c1d96fb9ed155a9932fa1a332f0f0a988da464bc`.
- Protected review aggregate: `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V4 bundle SHA-256: `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`.
- Semantic donor: `ce9db9e205865788d50933673a3e64d837a50f48`; reconciled through final V4
  head `f8050928715e536b60c42d800d1cbb81c085818f`.
- `review/**` was read-only throughout and is not part of either package commit.

The final V4 state, rather than an early checkpoint diff, was used as the semantic input. Later native
Receive, executor, EF, cache, metadata and RabbitMQ contracts in the product baseline remained
authoritative wherever they postdated the donor.

## Implemented ownership

The package closes runtime clock ownership across the following behavior families:

- relative send/publish and consume-context scheduling, recurring commands, delayed schedulers, saga
  scheduling and schedule-send local/UTC conversion;
- task timeouts, active requests, request-rate cancellation, concurrency limiting, batch timestamps and
  timers, job-service delays and bus readiness;
- message-data TTL, routing-slip timestamps, timestamp variables, command/event payloads, transforms,
  in-memory enqueue/move/dead-letter/error and outbox scheduling;
- Azure Service Bus scheduled enqueue and receive-lock timing, Azure Storage message-data expiration,
  SQL Server/PostgreSQL TTL, polling, maintenance and lock renewal, and Amazon SQS receive delay.

The final production scan contains 735 `TimeProvider` references. Only 12 direct process-clock reads in
11 files remain, all within the named architecture allowlist for external identity, diagnostics, probe/
activity/event metadata and RabbitMQ client diagnostics. Scheduler, timeout, expiration, polling, retry,
lock and TTL decisions are not allowlisted.

## Additional defect found during real-provider validation

The V4 donor calculated a relative scheduled time from the injected `FakeTimeProvider`, but
`ScheduleSendPipe` later subtracted the transport context clock. An EF outbox-created `SendContext` does
not carry the scheduler clock, so that subtraction fell back to the process clock. The real PostgreSQL
test `ScheduledPublish_RemainsDeferredUntilTheExactTransportDeadline` exposed the mismatch as a timeout.

The correction gives `ScheduleSendPipe` an optional explicit provider while preserving the existing
two-argument public behavior. `DelayedScheduleMessageProvider`, both delayed scheduler factory overloads
and `DelayedMessageSchedulerFilter` now pass one effective clock to both absolute-time calculation and
transport-delay calculation. Direct and factory-route tests prove the behavior for `IBus` and
`ISendEndpointProvider` even when the transport context has no clock payload. The previously failing real
PostgreSQL outbox case then passed 1/1.

## Native test ownership

New and strengthened tests use fixed epochs, controlled providers and positive completion barriers. They
cover relative scheduler overloads, recurring commands, context propagation, timeout success/failure,
command/event/concurrency timestamps, batching start/end/deadline semantics, routing slips, file and
Azure Storage TTL, job settings, Azure Service Bus delay/lock behavior and the direct-clock allowlist.

The Azure Storage tests are hermetic SDK-wire tests: a deterministic `HttpMessageHandler` records the
storage requests and responses, so no Azure subscription or live storage account is required. They prove
the injected clock, exact metadata value, one-minute minimum clamp and missing-TTL no-write case across
five executed cases. Live cloud validation is therefore not claimed.

All tests are source-owner native xUnit 4/MTP v2 tests. Requirements projections were updated for
Abstractions, Core, Architecture, Azure Service Bus and the new Azure Storage unit assembly. The new
Azure Storage product/test projects are included in the canonical Engineering and Unit solution graphs.

## Positive execution evidence

All commands used Release configuration and the repository's pinned/offline NuGet cache. Analyzer-active
builds completed with zero warnings and zero errors.

| Scope | Result |
|---|---:|
| Abstractions unit | 284/284 passed |
| Architecture | 159/159 passed |
| Core unit | 1,562/1,562 passed |
| Azure Storage unit | 5/5 passed |
| Azure Service Bus unit | 23/23 passed |
| Entity Framework Core unit | 102/102 passed |
| Amazon SQS unit | 77/77 passed |
| SQL Server unit | 39/39 passed |
| Canonical Unit/Architecture | 3,209/3,209 passed, 0 skipped |
| SQL Server real local profile | 60/60 passed, 0 skipped |
| Azure Service Bus emulator profile | 24/24 passed, 0 skipped |

The broad real LocalIntegration run executed 341 cases: 339 passed and two EF cases failed. One was the
new scheduled-publish defect described above and passed after correction. The other is the pre-existing
nondeterministic `InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet`.
It had already been reproduced against the clean pre-package-6 baseline. A fresh full EF run after this
package passed 58/59, with only that known unrelated case failing again and with a different race outcome.
The directly affected PostgreSQL transport and Amazon SQS LocalStack projects passed in the broad run.
No unrelated failure is hidden or reclassified as a package success.

## Mutation evidence

Each mutation was applied alone to buildable production code, its named owner went causally red, and the
target was restored before the next mutation and the final positive run.

| ID | One-cause mutation | Killing observation |
|---|---|---|
| M01 | Scheduler ignores injected clock | exact scheduled instant differed |
| M02 | Default recurring schedule uses process time | command timestamp differed |
| M03 | Endpoint recurring cancel uses process time | cancel timestamp differed |
| M04 | Publish recurring cancel uses process time | cancel timestamp differed |
| M05 | Routing-slip builder uses process time | exact creation timestamp differed |
| M06 | File message-data TTL uses process time | expiration differed |
| M07 | Request-rate algorithm uses process time | fake-clock timeout did not fire |
| M08 | Active request uses an unowned cancellation timer | controlled cancellation did not fire |
| M09 | Task timeout uses system timer | deterministic timeout owner failed |
| M10 | Batch first timestamp uses process time | start timestamp differed |
| M11 | Batch last timestamp uses process time | end timestamp differed |
| M12 | Batch timer ignores context provider | batch did not complete at deadline |
| M13 | Direct relative send uses system time | scheduled instant differed |
| M14 | Relative publish uses system time | scheduled instant differed |
| M15 | Scheduler-context relative send uses system time | scheduled instant differed |
| M16 | Consume-context relative send uses system time | scheduled instant differed |
| M17 | Azure Service Bus delay getter ignores provider | recovered delay differed |
| M18 | Azure Service Bus delay setter ignores provider | scheduled enqueue differed |
| M19 | Azure extension uses process time | scheduled enqueue differed |
| M20 | Azure receive lock uses process time | remaining lock duration differed |
| M21 | Azure Storage TTL uses process time | metadata expiration differed |
| M22 | Azure Storage omits minimum TTL clamp | zero/negative TTL cases differed |
| M23 | SQL Server polling uses system time | fake-time polling owner failed |
| M24 | From-last batch timer is not restarted | timer-change oracle detected no restart |
| M25 | Command payload uses process time | payload timestamp differed |
| M26 | Event payload uses process time | payload timestamp differed |
| M27 | Concurrency payload uses process time | payload timestamp differed |
| M28 | Schedule-send pipe ignores explicit scheduler clock | expected three hours became a multi-year delay |
| M29 | Endpoint-provider delayed factory drops clock | endpoint factory row failed; bus control stayed green |
| M30 | Bus delayed factory drops clock | bus factory row failed; endpoint control stayed green |

M09 and M23 initially needed independent two-second harness guards so that a surviving system-time mutant
failed promptly instead of hanging. M24 initially survived because the prior oracle observed only final
completion; `ObservableTimeProvider.ChangeCount` and `LastDueTime` were added to prove the required timer
restart directly. These are test-strengthening outcomes, not relaxed timeouts.

Restored mutation-target SHA-256 values are recorded for the intended final package implementation:

```text
ad8afc000caabe8ac66b499544e40ac0ad7dfd9b051420c00ab930cda7b884cd  DefaultRecurringSchedule.cs
2c235357cbf116b7444209bcd03c01e5f324807e40a445068671e8e9fcf3e8d5  RequestRateAlgorithm.cs
3eebb1818d3985f68472b2ccc7d3b5de5c74c6fbafe440b80f17d6bf96ec5884  ActiveRequest.cs
a09efae749da0f43de925ed4c588a1f8e37ff79bb2685bbc949eb3b213efbab4  TaskExtensions.cs
9238aede43372ac18d84299ed8d31ef23613298cf4091913b91bfcd8532278c3  BatchCollector.cs
9c5d139268c971d5f4524685259d79a4e6af9f85fe3a34313468d495f109de15  BatchConsumer.cs
2967e105585cfec51a4ec84e7f81a7e8cda3a4699eef598163b056369f3f4b32  ScheduleSendPipe.cs
35c8a8ca6e1b9e18fb8293e1957ac031fe6f587702d3c6104097dbb403578f92  MessageSchedulerBusExtensions.cs
11505018eb37c4bd8e65618422df4f1dc1e002697aa5daa6fd55999434bfbb95  AzureStorageMessageDataRepository.cs
```

## Static and hygiene gates

- The V4 bundle hash and complete history verify, including final head `f8050928`.
- `dotnet format --verify-no-changes` passes over every changed/new C# path. The first sandboxed invocation
  failed with the known Roslyn named-pipe permission denial; the identical escalated command passed after
  five whitespace-only corrections.
- All six changed/new requirements JSON files parse successfully.
- `git diff --check` passes.
- The complete production direct-clock allowlist architecture test passes.
- Empty obsolete cache directories were removed after their test ownership moved; the final source/test
  empty-directory scan passes.
- No protected review file is staged or modified.

Package status: locally complete as reviewer package 9/12 (75%). No remote publication is authorized or
implied by this evidence.
