# Product-wide profile: a30933c1c

Measured commit: `a30933c1c5c42ac5c3f37cba0c3a2b3d31c07843`.
Source tree: `ca0380dbe4dd0bc643c8eb9e6ded95ed4bbc1f43` (unchanged).
Test tree: `302fc92018220814acb3152ff01bf6acf2e474e4`.
Microsoft coverage-analysis workflow with canonical exact-commit receipts.
All 33 profiles pass, 12,947 executions; all four provider fixture groups exit 0.
SQL Server passes 75/75, no skips. No failed T44 profile was replaced by a retry.

| Metric | T43 | T44 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,485/93,749 | 85,532/93,749 |
| Line coverage | 91.18497% | 91.23511% |
| Covered/valid conservative branches | 30,839/36,843 | 30,858/36,843 |
| Conservative branch coverage | 83.70382% | 83.75539% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,432 | 4,423 |
| Zero / partial line coverage | 2,756 / 1,676 | 2,755 / 1,668 |
| Additional branch-only gaps | 1,514 | 1,510 |
| Union of gap candidates | 5,946 | 5,933 |

## Behavior and target evidence

[T44 tests and counterprobes](t44-ef-store-contracts.md) bind eighteen additional
real SQLite cases to nine requirement variants. The packet covers composite
inbox pagination, stale/missing durable transitions, failed initialization,
admission/release failure atomicity, capacity boundaries and scheduling override.
No production behavior changed or reproduced product defect is claimed.

| EF reliable-store method | T43 lines / branches / CRAP | T44 lines / branches / CRAP |
| --- | --- | --- |
| AdmitAsync | 28/47 / 5/12 / 21.51327 | 40/47 / 8/12 / 12.47573 |
| EnsureInitializedAsync | 38/45 / 9/12 / 12.54202 | 44/45 / 10/12 / 12.00158 |
| MarkDeliveredAsync | 11/16 / 1/4 / 4.48828 | 16/16 / 4/4 / 4 |
| GetQuarantineAsync (inbox) | 24/33 / 2/4 / 4.32457 | 33/33 / 4/4 / 4 |
| MutateOwnedAsync | 13/15 / 3/4 / 4.03793 | 15/15 / 4/4 / 4 |
| DecrementCapacityAsync | 5/6 / 1/2 / 2.01852 | 6/6 / 2/2 / 2 |
| ScheduleAsync | 0/2 / 0/0 / 2 | 2/2 / 0/0 / 1 |

## Reconciliation and remaining gaps

Source files and all method identities are unchanged. Ten comparable line gaps
close and one opens. Five closures are the target methods MarkDeliveredAsync,
GetQuarantineAsync (inbox), MutateOwnedAsync, DecrementCapacityAsync and
ScheduleAsync. Non-target closures are TypeConverterCache.GetOrCreateConverter,
ReliableMessagingDeliveryService.ExecuteAsync, InMemoryDelayProvider.RearmTimer,
TaskExtensions.OrCanceledAsync and SagaInstance.MarkInUseAsync. The new gap is
BatchConsumer.TimeLimitExpired at line 91.

Physical line observations reconcile as 48 gains and one loss, net +47:
37 gains in the target store, three in InboxCleanupService, three in SagaInstance,
two in TaskExtensions, one each in InMemoryDelayProvider, TypeConverterCache and
ReliableMessagingDeliveryService; one loss in BatchConsumer. No source lines are
unmatched, added or removed. Non-target changes are observations, not causal
claims about the added EF tests.

Branch-only identities have six closures and two openings. Closures include
one MessageReceiver.HandleAsync overload, both Courier GetScopeAsync methods,
ProcessorContextFactory.CreateSharedConnectionAsync, SagaRegistrationExtensions
AddSagas and PlanSagaRegistrations. Openings are the other MessageReceiver
HandleAsync subscription overload (1/2) and PublishEndpoint's generated
PublishInternalAsync helper (2/4). The closed queue overload now has 2/2 branches.
Physical conservative branch gains are +11 in the target file and net +8 in
other files, total +19.
Exact identities/signatures remain in the local inventories. Conservative branch
counts are observation bounds, not a union of stable branch IDs.

Highest CRAP entries with remaining line gaps:

| Method | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 66/70 | 28 | 28.14629 |

Full remaining inventories are `artifacts/t44-method-gaps.json` and
`artifacts/t44-branch-only-gaps.json`. They include compiler-generated identities.
CRAP<=30 does not establish A+. All other unclosed requirements remain open.

## Evidence integrity

Canonical aggregation and explicit old/new identity/physical-line reconciliation
pass. Independent read-only audit confirms 487 file hashes, 66 runner/settings
bindings, 33 profiles/32 assemblies, commit/tree identities and all four fixture
groups. Independent aggregation of all 66 old/new XML reports agrees exactly
with the line/branch/CRAP totals, gap inventories and physical comparison.
All six artifact hashes below and the target-method table are confirmed.
No concrete accounting blocker remains; this is not a global A+ declaration.

Local ignored artifacts, SHA-256:

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t44-aggregate.json | 94ec6638a147077436234db3180d1d4423d8a28a5dabc6635ba456e6d25f3673 |
| t44-all-methods.json | 2aa19971a0ea2e49ff86610e4e3bf0ed6fdb988ab4b6e57b0b1df1678806dda9 |
| t44-method-gaps.json | 89d94386545b82ecb7a88ec6c08b863d8d7ea953a9731f2c86f86e30db59f575 |
| t44-branch-only-gaps.json | c85321c607ad82c134cb7db766cbfbc2313c013469d72f1136f0c204b74ecb69 |
| t44-profile-progress.json | 208a6c8f5438e5c201ae21138e49959f44d639dce333bde59591b7f1509b6200 |
| t44-gap-delta.json | e52590117da72fa864a25ea622a4452886fe3dd6bc95a078391faf5df4a532fc |

## Next coherent packet

InMemory reliable-inbox pipeline research identifies additional admission,
delayed-send and removed/stale-owner failure contracts. Existing consumer commit
atomicity, intent conflict and basic retry/cancellation tests must not be repeated.
Successful callbacks must reach actual SetConsumedAsync before claiming commit.
Use deterministic clock/handshake evidence, account for background delivery and
avoid artificial missing-runtime/proof configurations solely for line coverage.
The next packet starts after T44 audit, documentation and push are complete.
The Roslyn all-repository API/comment audit remains after the coverage/CRAP goal.
