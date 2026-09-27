# Product-wide profile: c28e9feb4

Measured commit: `c28e9feb41f8146869e33b0f361ecd3e920f581b`.
Source tree: `ca0380dbe4dd0bc643c8eb9e6ded95ed4bbc1f43`.
Test tree: `bac755743fabd1f96eae2ca424044b1a635bbf97`.
Microsoft coverage-analysis workflow; canonical exact-commit receipts and full
provider measurement. All 33 profiles verified, 32 expected assemblies, 12,923
passing executions. All four fixture groups exit 0, including cleanup. SQL Server
passes 75/75 without skips. No retry replaced a failed T42 profile.

| Metric | T41 | T42 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,401/93,751 | 85,480/93,749 |
| Line coverage | 91.09343% | 91.17964% |
| Covered/valid conservative branches | 30,808/36,823 | 30,840/36,843 |
| Conservative branch coverage | 83.66510% | 83.70654% |
| Method identities | 26,060 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,445 | 4,432 |
| Zero / partial line coverage | 2,769 / 1,676 | 2,756 / 1,676 |
| Additional branch-only gaps | 1,510 | 1,511 |
| Union of gap candidates | 5,955 | 5,943 |

## Target evidence and limits

See [T42 behavior tests and counterprobes](t42-functions-receiver-isolation.md).
All seven public dispatch overloads now have full observed line coverage.
The file improves from 25/80 to 73/78 covered physical lines.

| Method | Lines | Conservative branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| CreateMessageReceiver, queue | 8/10 | 2/4 | 4 / 4.128 |
| CreateMessageReceiver, subscription | 12/14 | 2/4 | 4 / 4.04665 |
| ConfigureAll | 4/4 | 4/4 | 4 / 4 |
| CreateRegistrationContext | 2/2 | 1/2 | 2 / 2 |

MessageReceiver conservative branches improve from 5/16 to 29/36. Seven branches
remain unobserved: constructor selection, the compiler-observed branch on
subscription all-handler dispatch,
queue/topic validation and private callback guards, and custom registration
context fallback. Four uncovered physical guard lines remain at 160/162/180/182.
Ten additional zero-covered method identities are compiler-generated getter/setter
accessors on the private ReceiverKey record, all mapped to line 223. These remain
in the inventory; they are not ten separately established product-contract defects.
The file has 12 line-gap identities in total. No reflection tests were added merely
to invoke private guards or generated accessors.

The tests establish local receiver behavior, not broker startup, settlement or
event delivery. Standard default/typed DI ownership is proved; external custom
registration-context fallback is not. Global A+ remains open.

## Remaining risk hotspots

The largest CRAP values are 30 with full line coverage; full line coverage does
not prove branch or assertion completeness. Highest CRAP entries with line gaps:

| Method | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 66/70 | 28 | 28.14629 |

The complete remaining worklists are `artifacts/t42-method-gaps.json` and
`artifacts/t42-branch-only-gaps.json`, with hashes below. Prioritize real delivery,
ownership, persistence and cancellation contracts. The next candidate is stale
and missing-record ownership transitions in EntityFrameworkReliableStore; analysis
is preliminary, with no unexecuted mutation claimed as a surviving fault.

## Reconciled comparison

Among unchanged method identities, 15 line gaps close and one opens. Separately,
14 identities disappear and 25 appear, exclusively in MessageReceiver; 11 removed
and 12 added identities have line gaps. Thus 4,445 - 15 + 1 - 11 + 12 = 4,432.
Changed private helper signatures and generated lambdas are not counted as
closed gaps. The new comparable gap is generic OrCanceledAsync, lines 52/53.

Closures outside MessageReceiver occur in SubscriptionEndpointSettings,
RegistrationContextExtensions.ConfigureConsumers, RegistrationContext.ConfigureSagas,
ServiceBusHostConfiguration.CreateSubscriptionEndpointConfiguration,
SubscriptionBrokeredMessageReceiverConfiguration constructor/Build,
MultiBusInstance.HostConfiguration, ReliableMessagingDeliveryService and
Agent.SetCompleted. Not every non-target observation is causally attributed to T42.

Physical unchanged-text mapping finds 63 newly observed and 2 no-longer-observed
lines. There are 26 unmatched old lines (5 covered) and 24 unmatched new lines
(23 covered): covered total changes by +79 and denominator by -2. MessageReceiver
contributes +48 covered physical lines: 30 mapped gains plus 23 newly mapped lines
minus 5 old covered lines. Other files contribute net +31. Equal source text does
not establish semantic equivalence, and removed lines are not test gains.

MessageReceiver conservative branches change by +24 covered/+20 valid; other
files contribute net +8 covered with unchanged denominator. All observations,
including the new OrCanceledAsync gap, remain recorded in the worklists.

## Evidence integrity

Independent read-only review confirms 487 file hashes, 66 runner/settings bindings,
all profile/assembly identities and exact commit/tree bindings. Independent
physical-line OR counts and conservative branch counts from all 66 old/new XML
reports agree with the aggregates. Method identity/gap and physical-line delta
reconciliation agree with the completed delta artifact. No accounting blocker.
Conservative branch observations are not a branch-ID union. Method inventories
include compiler-generated identities. Zero CRAP above 30 alone is not global A+.

Local artifacts are ignored; SHA-256 evidence:

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t42-aggregate.json | 4678258f55046c0b2ce91704d13fe695916882725f1e5a473be390c454063f65 |
| t42-all-methods.json | 4c879cb5435527d94729378aa1c7cc3a5c66973a0a3c9343d90f55f85e649fe4 |
| t42-method-gaps.json | ba9e117cfcd485a23caceac24f8cac539a22a9f382454e1bff63b82fba68ca21 |
| t42-branch-only-gaps.json | dab845d7fffa3f3dab929763a80e5c7adb4514852e4a817c881abd0adf5263a9 |
| t42-profile-progress.json | d0e029406f4b2f7099d97e88fdf15f1f4b76367c5435566eb87895f338b53218 |
| t42-gap-delta.json | a928a2e04658aaa43f74a7d719bf1f03792716e231b324ba296a23ab535a3ea2 |

Roslyn API/comment audit follows completion of the coverage and CRAP objective.
