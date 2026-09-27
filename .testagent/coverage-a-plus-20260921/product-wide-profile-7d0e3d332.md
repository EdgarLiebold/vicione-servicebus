# Product-wide profile: 7d0e3d332

Measured commit: `7d0e3d332b935cdb4e5ddbcd883c9bd15a73b26e`.
Source tree: `6dc2700433374e9108821fd2a16e2f76404108a2` (unchanged from T46).
Test tree: `e18908a281d06323491bac1655ba966525e3d88c`.
Microsoft coverage-analysis workflow with canonical exact-commit receipts.
All 33 profiles pass, 13,003 executions across 32 measured assemblies, no skips
or failed profile. All four provider fixture groups exit 0 with empty findings.
This was one complete measurement, with no profile retry.

| Metric | T46 | T47 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,587/93,753 | 85,587/93,753 |
| Line coverage | 91.28988% | 91.28988% |
| Covered/valid conservative branches | 30,903/36,845 | 30,906/36,845 |
| Conservative branch coverage | 83.87298% | 83.88112% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,409 | 4,403 |
| Zero / partial line coverage | 2,751 / 1,658 | 2,749 / 1,654 |
| Additional branch-only gaps | 1,508 | 1,507 |
| Union of gap candidates | 5,917 | 5,910 |

## Behavioral evidence

[T47 tests and counterprobes](t47-saga-journeys.md) bind ten new cases to two
requirement variants in two test projects. Seven real-transport journeys verify
Saga/event ownership of send callbacks, exact delivered payload and metadata,
pending factory/send stages, primary dispatch failures, successful compensation
and a failure inside the recovery callback. Two independently primed Sagas prevent
shared-context mistakes from passing. Exact traces and drained message counts
also reject premature continuation and duplicate delivery.

Three Quartz journeys reuse a Saga for two request generations. Late response,
fault and timeout messages from Request1 cannot change Request2 or enqueue a
cancellation for its trigger. Both real triggers are positively observed before
their cancellation and absent after confirmed cleanup. An exact input-address
and message-ID receive-completion barrier precedes negative assertions.
The old timeout is deliberately injected as a late message; this does not claim
a second Quartz firing of the already deleted first trigger.

Two isolated faults were detected: bypassing the callback pipe fails 7/7 cases
on the exact delivered CorrelationId; correlating the old timeout by Saga-ID
fails its pending-state assertion while response/fault controls pass (1/3).
Both source files were SHA-restored. Combined restored verification passes
20/20 Core and 22/22 Quartz, zero build warnings/errors. All 5,873 current
source/test paths compare byte-identically between MAIN and the isolated checkout.
Final full profiles pass 6,744 Core and 284 Quartz cases.

## Reconciliation

Product source bytes, method identities and physical line denominators are
unchanged. Ten physical lines become covered and ten lose their prior observation,
so global line coverage is unchanged. Six gains occur directly in the target
files: SendCallbackExtensions lines 340/742/743 and request configuration callbacks
at ViciOneServiceBusStateMachine lines 1342/1347/1352.

Other gains are TaskExtensions 52/53, ResourceCache.Creation 193 and
InMemoryDelayProvider 225. Lost observations are EventHubDataReceiver 85,
PostgreSqlDbConnectionContext 394/396, FutureExtensions 38/39, JobService 203,
SagaInstance 109/110/112 and ServiceBusInstrumentation 356. No causal attribution
of these other paths to the new tests is made, and no new product defect follows
from the observation changes alone.

Eight line-gap identities close and two appear. Five closures directly match
the test additions: two SendCallback overloads and three dedicated-request-ID
configuration callbacks. Other closures are TaskExtensions.OrCanceledAsync,
InMemoryDelayProvider.RearmTimer and the request callback at line 1383 without a
dedicated request-ID property. That last callback still has 1/2 branches and moves
from line-gap to branch-only; it is neither complete nor a direct generation-test
proof. New line gaps are FutureExtensions.AddSubscription's callback and
SagaInstance.MarkInUseAsync.

Two previous branch-only entries disappear (a generated PublishEndpoint helper
and SagaRegistrationExtensions.AddSagas), while the line-1383 callback enters
that list. StateMachine physical branches gain four observations, including the
extra line-1383 path; other files net minus one, giving the overall plus three.
Conservative branch counts are per-line observations, not a stable branch-ID union.

## Remaining risks and limits

| Method with remaining line gaps | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 68/70 | 28 | 28.01829 |

The complete gap inventories are `artifacts/t47-method-gaps.json` and
`artifacts/t47-branch-only-gaps.json`, including compiler-generated identities.
Their 5,910-entry union is a remaining behavioral assessment worklist, not 5,910
proven defects. No numeric A+ line/branch percentage has been agreed; zero CRAP
values above 30 and green profiles do not establish global A+.

The new journeys exercise in-memory Saga storage and transport with real Quartz.
They do not establish durable Saga persistence, an external broker roundtrip or
automatic interruption of an arbitrary pending factory by cancellation.
Existing provider profiles retain their own acceptance scope. T46's direct
non-generic enumeration-wrapper gaps remain documented separately.

## Independent audit and integrity

The final read-only audit finds no blocking implementation or accounting issue.
It verifies 487 receipt/binary/log/report hashes, 66 runner/settings bindings,
nine broker-log hashes, all terminal test counts, source/test trees and empty
fixture findings. Independent reconstruction of all 66 T46/T47 XML reports
exactly matches physical totals, every method row, CRAP values, gap inventories
and the reconciliation above. This is packet acceptance, not global A+ approval.

Verify-only formatting loads both target test projects and exits 0 without edits.
The first attempt against the product-only solution is explicitly rejected as
evidence. Initial fixture compile and exception-expectation failures are retained
in the packet record; successful restored runs are the acceptance evidence.

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t47-aggregate.json | 1a871a3aa95efa793ff7614682efbe1abb4ae4b7a1a2b1c90992ae4721b694df |
| t47-all-methods.json | df754e2bfbce864a78b8e241e6e5e448f9fab6e58d7b100bf50b9b77be00f5e2 |
| t47-method-gaps.json | 0adb12069d5560bf69e9c9af7fbcd7efbf1d31671ce018fed478bfe39104f18a |
| t47-branch-only-gaps.json | 6d7ef99d793869f243d544c0f6abc4d9a2d23c745333f34e463fc72ce9d477bf |
| t47-profile-progress.json | 6edf067c41ae9ac38447691c3297545b85b0d9acc48dc4a68cc5bed04acaddfa |
| t47-gap-delta.json | dd22706854ae695d8731e594fb27766f5406beb9e20144ed64b738e5c41415e1 |

## Continuation

Publish the completed packet before the next implementation package. Retain the
PO's larger coherent packages and one final full measurement per package. The
first source reading remains complete. The all-repository Roslyn API/comment
audit follows completion of the coverage/CRAP goal; global A+ remains open.
