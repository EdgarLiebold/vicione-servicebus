# Product-wide profile: a763757b4

Measured commit: `a763757b48083befb64cd8e3e7ff3c39304b819f`.
Source tree: `ca0380dbe4dd0bc643c8eb9e6ded95ed4bbc1f43` (unchanged).
Test tree: `21984b8b18346f141b46a70b4edebb66c29e52a0`.
Microsoft coverage-analysis workflow; canonical exact-commit receipts and full
provider measurement. All 33 profiles verified, 32 expected assemblies, 12,929
passing executions. All four fixture groups exit 0, including cleanup. SQL Server
passes 75/75 without skips. No retry replaced a failed T43 profile.

| Metric | T42 | T43 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,480/93,749 | 85,485/93,749 |
| Line coverage | 91.17964% | 91.18497% |
| Covered/valid conservative branches | 30,840/36,843 | 30,839/36,843 |
| Conservative branch coverage | 83.70654% | 83.70382% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,432 | 4,432 |
| Zero / partial line coverage | 2,756 / 1,676 | 2,756 / 1,676 |
| Additional branch-only gaps | 1,511 | 1,514 |
| Union of gap candidates | 5,943 | 5,946 |

## Behavior and target evidence

See [T43 behavior tests and counterprobes](t43-inbox-ownership-transitions.md).
Six real SQLite cases prove stale and missing inbox consumer transitions for
completion, retry and quarantine, preserving the current owner and all neighbors.
The unchanged product passes these cases; no reproduced product bug is claimed.

| Method | T42 lines / branches / CRAP | T43 lines / branches / CRAP |
| --- | --- | --- |
| RequireOwnedOrMissingAsync | 3/9 / 1/4 / 8.74074 | 9/9 / 4/4 / 4 |
| CompleteAsync (inbox) | 10/10 / 0/0 / 1 | 10/10 / 0/0 / 1 |
| ScheduleRetryAsync (inbox) | 10/10 / 0/0 / 1 | 10/10 / 0/0 / 1 |
| QuarantineAsync (inbox) | 10/10 / 0/0 / 1 | 10/10 / 0/0 / 1 |

The three entrypoints already had full observed line coverage. New failure/state
assertions and five detected counterprobes improve their behavioral protection.
SQLite evidence does not prove concurrent PostgreSQL/SQL Server isolation.
Individual update identity-filter mutations may be masked by unique lease tokens;
only the documented counterprobes have empirical kill evidence.

## Reconciled comparison and remaining gaps

No source files or method identities changed. Two comparable line gaps close:
RequireOwnedOrMissingAsync and BatchConsumer.TimeLimitExpired. Two open:
TypeConverterCache.GetOrCreateConverter (line 54) and
ReliableMessagingDeliveryService.ExecuteAsync (line 83). Generic OrCanceledAsync
remains at 5/7 lines; it is not a new T43 gap.

Seven newly observed physical lines are EF store 1023-1027/1029 and BatchConsumer
91. The two losses above give net +5 covered lines. There are no unmatched,
removed or added physical source lines. Non-target observations are not causally
attributed to this packet.

The target helper gains 3 conservative branches. In the same EF file,
MutateInboxQuarantineAsync gains one branch (7/8 to 8/8). Other files have net -5:
BatchConsumer +1; ProcessorContextFactory, SqlReceiveLockContext,
CompensateActivityScopeProvider, ExecuteActivityScopeProvider, TypeConverterCache
and ReliableMessagingDeliveryService each -1. Total +3 +1 -5 = -1. These are
physical branch observations, distinct from method-level gap classifications.

Additional branch-only method gaps change by five new/two closed:

| Method | T43 observation | Change |
| --- | ---: | --- |
| ExecuteActivityScopeProvider.GetScopeAsync | 7/8 | New branch-only gap |
| CompensateActivityScopeProvider.GetScopeAsync | 7/8 | New branch-only gap |
| SagaRegistrationExtensions.PlanSagaRegistrations | 3/4 | New branch-only gap |
| MessageReceiver.HandleAsync (queue) | 1/2 | New branch-only gap |
| ProcessorContextFactory.CreateSharedConnectionAsync | 1/2 | New branch-only gap |
| EntityFrameworkReliableStore.MutateInboxQuarantineAsync | 8/8 | Closed |
| MessageReceiver.HandleAsync (subscription) | 2/2 | Closed |

All observations remain in `artifacts/t43-method-gaps.json` and
`artifacts/t43-branch-only-gaps.json`; the method inventory retains generated
identities. No global A+ claim follows from a complete target helper or CRAP<=30.

Highest CRAP entries with remaining line gaps are unchanged:

| Method | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 66/70 | 28 | 28.14629 |

## Evidence integrity

Independent read-only audit confirms 487 file hashes, 66 runner/settings bindings,
all 33 profile/32 assembly identities and commit/tree bindings. Independent totals
from all 66 old/new XML reports agree with both aggregates. Source/identity and
physical-line reconciliation are exact. Conservative branches remain an observation
bound, not a branch-ID union.

Local artifacts are ignored; SHA-256 evidence:

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t43-aggregate.json | 1a7aaf2d94e9099ab1f98504acbc08d72d4ed0f9c293469e239427abca6d5951 |
| t43-all-methods.json | d9a78044b6cadfe0bd480fb53a6e94929b0bacc1ba96c6f5a821d4d318304837 |
| t43-method-gaps.json | 90d1a0bf6f20ed856fa2ec074614cf198fcf038fdd6f7eca015fca16ed011942 |
| t43-branch-only-gaps.json | 07bf58428f722cc17de81fa358d7bf0674b6e3fec92d11ac43ff54b34965da1a |
| t43-profile-progress.json | 68833b87025dafd51a43bbee87d33411d4065847c084bd4cbc1deaca62a42c90 |
| t43-gap-delta.json | 71d6586316389e733f41d4558a3478d2d6d84b5f91c64c67af9d51bbf42c42e3 |

## Next iteration scope

Following the user's efficiency feedback, larger coherent behavior packets precede
one full measurement. The next EF-store packet combines inbox pagination,
durable-send stale/missing ownership, capacity/admission failure atomicity and
same-instance recovery, and scheduling due-time override/immutability. Narrow
tests, read-only adversarial reviews and isolated counterprobes remain mandatory
during implementation. Existing lifecycle/concurrency tests are not duplicated.
Roslyn all-repository API/comment audit follows the coverage/CRAP objective.
