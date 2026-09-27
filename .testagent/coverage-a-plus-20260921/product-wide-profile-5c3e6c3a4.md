# Product-wide profile: 5c3e6c3a4

Measured commit: `5c3e6c3a4cea65458d47c417565335f4eeadb540`.
Source tree: `ca0380dbe4dd0bc643c8eb9e6ded95ed4bbc1f43` (unchanged).
Test tree: `b6cfe4607c459be2840a54a5e2d89325c016c816`.
Microsoft coverage-analysis workflow with canonical exact-commit receipts.
All 33 profiles pass, 12,953 executions; all four provider fixture groups exit0.
No failed measurement profile was replaced by a retry.

| Metric | T44 | T45 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,532/93,749 | 85,536/93,749 |
| Line coverage | 91.23511% | 91.23937% |
| Covered/valid conservative branches | 30,858/36,843 | 30,862/36,843 |
| Conservative branch coverage | 83.75539% | 83.76625% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,423 | 4,424 |
| Zero / partial line coverage | 2,755 / 1,668 | 2,755 / 1,669 |
| Additional branch-only gaps | 1,510 | 1,514 |
| Union of gap candidates | 5,933 | 5,938 |

## Behavior and target evidence

[T45 tests and counterprobes](t45-inmemory-inbox-pipeline.md) bind six new cases
to three requirement variants: removed/stale owner failure, delayed consumer
commit and admission rejection followed by controlled operator recovery.
Four valid deliberate faults were detected and restored. The preliminary
misdirected lease probe is explicitly excluded from inbox evidence.
The isolated full restoration exposed a race in a new test's global quarantine
expectation; the corrected test verifies the recovered inner key and the
enclosing consumer's separately retained failure after shutdown. Corrected
isolated focused tests pass15/15; final full33 validates the committed correction.
No product defect or product source change is claimed.

| InMemory inbox method | T44 lines / branches / CRAP | T45 lines / branches / CRAP |
| --- | --- | --- |
| Factory.SendAsync | 66/70 / 20/28 / 28.14629 | 68/70 / 24/28 / 28.01829 |
| Context.AddSendAsync | 32/40 / 12/22 / 25.872 | 32/40 / 13/22 / 25.872 |

## Reconciliation

Source and method identities are unchanged. Four line gaps close: inbox-store
QuarantineAsync and ScheduleRetryAsync, OutboxSendEndpoint.AddSendAsync and
ClientRequestHandle.SendAsync. Five appear: AzureTableMessageJournalStore
GetOrCreateLeaseAsync, SagaInstance.MarkInUseAsync, InMemoryDelayProvider.RearmTimer,
ReliableMessagingDeliveryService.ExecuteAsync and TaskExtensions.OrCanceledAsync.
Physical observations are17 gained and13 lost lines, net+4, with no unmatched
source lines. Gains include2 factory lines,7 inbox-store lines,4 outbox-send lines
and4 request-client lines. Losses are outside the selected inbox implementation.
These observations do not establish causality for non-target behavior.

Two branch-only entries close (inbox ResolveOperationCancellationToken and a
PublishInternalAsync helper), six appear (Saga registration's AddSagas and
PlanSagaRegistrations, both Courier scope providers, EF MutateInboxQuarantineAsync
and OutboxSendEndpoint.AddSendAsync). The latter moved from a line gap to a
branch-only gap:14/14lines,10/14branches. It is not newly unexecuted code.

## Remaining risk

| Method with remaining line gaps | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 68/70 | 28 | 28.01829 |

Full remaining inventories are `artifacts/t45-method-gaps.json` and
`artifacts/t45-branch-only-gaps.json`; compiler-generated identities are included.
Conservative branches are observation bounds, not a union of stable branch IDs.
CRAP<=30 and passing profiles do not establish A+. All unclosed requirements
remain open. Net measurement changes are not all attributed to this packet.

## Evidence integrity

Canonical aggregate and detailed old/new reconciliation are complete.
Independent read-only audit confirms487hashes,66runner/settings bindings,
33profiles,32assemblies, exact commit/trees and all four fixture groups.
All66old/new XML reports were independently aggregated with exact agreement on
lines, conservative branches, CRAP, identities and gap inventories. No concrete
accounting blocker remains. Branch gains are factory+4,context+2,inbox-store+4,
outbox-send+2; other areas net-8, yielding global+4. This is no global A+ approval.
Local evidence: `artifacts/t45-profile-progress.json`, `t45-aggregate.json`,
`t45-all-methods.json`, `t45-method-gaps.json`, `t45-branch-only-gaps.json`
and `t45-gap-delta.json` (all under artifacts/).

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t45-aggregate.json | 668a543fb2411af1a0eb945ddb12377098602c61eaa2fdf1e5343e53da7bb1bc |
| t45-all-methods.json | 6c9d5fc8e8a1418bdfead8ce025c807bdfe503bfd3be3453f648d843d2de489c |
| t45-method-gaps.json | ef6f066043c8b8e02310e7d44320a46c959a1e7418e67ad8ecdda5c93e7926c2 |
| t45-branch-only-gaps.json | b8f89e5b015aba19862e5935fefcbc30d1bf353692830b6454c2bfecfd23ba07 |
| t45-profile-progress.json | 46aa84e317f28d4a00d044dc240f7ec65c1e87e02df0b68e93587cc3c9e16d65 |
| t45-gap-delta.json | 310e8a2db9a3a60833aed6f29a77e8d0e199de19077d1c42501523b3506e9335 |

## Next coherent packet

Bounded transport-header research identifies native value preservation,
overwrite/removal semantics, typed SNS/SQS filter handling and absent RabbitMQ
timestamp spoofing as candidates. Existing round-trip and metadata precedence
tests must not be duplicated. The PO explicitly requests larger packets: these
related contracts across ActiveMQ, RabbitMQ and SNS/SQS form one packet with
focused tests while implementing and one final full33 measurement and push.
One existing overwrite test has a statically
identified intermediate-state assertion gap; no surviving mutant is claimed
until experimentally checked. Implementation starts after T45 closure and push.
The all-repository Roslyn API/comment audit remains after the coverage/CRAP goal.
