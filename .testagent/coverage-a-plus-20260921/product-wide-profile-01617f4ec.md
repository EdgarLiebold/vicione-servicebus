# Product-wide profile: 01617f4ec

Measured commit: `01617f4eca3d3b79e6cd9a348f37eca7d8905b4a`.
Source tree: `6dc2700433374e9108821fd2a16e2f76404108a2`.
Test tree: `0c9c23a7c759a4fae4f9504d7bf8f0defd66b525`.
Microsoft coverage-analysis workflow with canonical exact-commit receipts.
All 33 profiles pass, with 12,993 executions across 32 measured assemblies.
All four provider fixture groups exit 0. No measurement profile was retried.

| Metric | T45 | T46 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,536/93,749 | 85,587/93,753 |
| Line coverage | 91.23937% | 91.28988% |
| Covered/valid conservative branches | 30,862/36,843 | 30,903/36,845 |
| Conservative branch coverage | 83.76625% | 83.87298% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,424 | 4,409 |
| Zero / partial line coverage | 2,755 / 1,669 | 2,751 / 1,658 |
| Additional branch-only gaps | 1,514 | 1,508 |
| Union of gap candidates | 5,938 | 5,917 |

## Behavior and target evidence

[T46 tests and counterprobes](t46-transport-headers.md) bind 40 additional cases
across ActiveMQ, RabbitMQ and SNS/SQS to 15 requirement variants. The product
correction converts OpenWire Local timestamps to UTC before epoch comparison;
real OpenWire and AMQP objects verify exact instants and milliseconds.
Native-map updates preserve neighbors and wire types. RabbitMQ checks preserve
reserved metadata and intermediate overwrite states. SNS/SQS checks observe
normalized filter arguments and exact permitted or rejected provider results.

Four isolated single-cause faults were detected: timestamp normalization,
RabbitMQ overwrite protection and each Amazon provider's configured filter.
Restored controls pass 21/21 ActiveMQ, 21/21 RabbitMQ and 13/13 SNS/SQS,
including requirement projections. All 5,871 source/test paths in MAIN and
the restored isolated checkout were byte-identical before the commit.
Final full profiles pass 226 ActiveMQ, 490 RabbitMQ and 314 SNS/SQS cases.

| Selected method | T45 lines / branches / CRAP | T46 lines / branches / CRAP |
| --- | --- | --- |
| ActiveMqHeaderProvider.TryGetHeader | 13/16 / 7/10 / 10.65918 | 20/20 / 12/12 / 12 |
| PrimitiveMapHeaders.Set(object, overwrite) | 4/9 / 3/10 / 27.14678 | 9/9 / 10/10 / 10 |
| PrimitiveMapHeaders.TryGetHeader | 0/6 / 0/2 / 6 | 6/6 / 2/2 / 2 |
| RabbitMqHeaderProvider.TryGetHeader | 32/34 / 25/26 / 26.13759 | 34/34 / 26/26 / 26 |
| RabbitMqHeaderProvider.TryNormalize | 3/7 / 1/4 / 6.98542 | 7/7 / 4/4 / 4 |
| SNS typed TryConvert | 3/5 / 2/4 / 5.024 | 5/5 / 4/4 / 4 |
| SQS typed TryConvert | 3/5 / 2/4 / 5.024 | 5/5 / 4/4 / 4 |

The corrected ActiveMQ method has higher complexity (12 versus 10) because it
handles Local timestamps explicitly; full execution keeps CRAP at complexity.
This increase accompanies the product fix and is not hidden by the coverage gain.

## Reconciliation

Only ActiveMqHeaderProvider product source changed. Method identities are
unchanged; line mapping accounts for the edit rather than comparing raw line
numbers. There are 46 newly observed covered lines with unchanged text and one
lost observation (ResourceCache.Creation). Two previously uncovered edited lines
have no textual match; six new edited lines are covered. The resulting physical
delta is +51 covered and +4 valid lines.

Target unchanged-text gains are PrimitiveMap 25, ActiveMQ provider 1, SNS 2,
SQS 2, RabbitMQ move headers 3 and RabbitMQ provider 6. Including the six edited
covered lines gives +45 covered target lines. Non-target gains are Azure Table
2, EventHubDataReceiver 1, JobService 1 and SagaInstance 3; the cache loss yields
another net +6. Those non-target observations are not attributed to these tests.

Sixteen line gaps close: fourteen target methods and two non-target methods
(SagaInstance.MarkInUseAsync and AzureTableMessageJournalStore.GetOrCreateLeaseAsync).
One Saga request callback becomes a line-gap entry at line 1383. That callback
was previously branch-only, so its disappearance from the branch-only list is
not an improvement. Six other branch-only entries close and one PublishEndpoint
helper becomes branch-only, producing the net reduction of six.

Target conservative branch gains total 38: PrimitiveMap 19, ActiveMQ provider 5,
RabbitMQ move headers 6, RabbitMQ provider 4, SNS 2 and SQS 2. Other sources net
another 3; total +41 covered branches. The source correction adds two valid
branches. These are conservative per-line observations, not a union of stable
branch IDs, and do not prove complete semantic branch coverage.

## Remaining risk and proof limits

| Method with remaining line gaps | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 68/70 | 28 | 28.01829 |

The complete inventories remain in `artifacts/t46-method-gaps.json` and
`artifacts/t46-branch-only-gaps.json`, including compiler-generated identities.
No numeric A+ line/branch percentage has been agreed. Passing profiles and zero
CRAP values above 30 do not close the outstanding behavioral gap assessment.

Both PrimitiveMapHeaders and MoveTransportHeaders explicit non-generic
IEnumerable.GetEnumerator wrappers remain at 0/1 observed lines. The tests'
`Cast<HeaderValue>()` calls may select the generic fast path; they prove LINQ
consumer values, not direct non-generic dispatch. These simple delegating
wrappers remain visible as unverified entries. RabbitMQ MoveTransportHeaders
object Set also retains one conservative branch gap (11/12). No extra test was
added merely to change those numbers. SDK-object tests do not themselves prove
broker delivery or AMQP wire roundtrips; full provider profiles are separate.

## Evidence integrity

The pre-freeze independent read-only review confirmed 11 counterprobe/control
log hashes and counts, four restoration hashes and the requirement bindings.
Format verification passed; its documented workspace warning concerns an
unrelated analyzer metadata reference, with all target projects loaded.
The final independent audit confirms 487 hashes, 66 runner/settings bindings,
33 profiles, 32 assemblies, exact source/test trees and four empty fixture
finding lists. All 66 T45/T46 XML reports were independently reconstructed with
exact agreement on totals, identities, gap inventories and source-mapped changes.
No concrete integrity or accounting blocker remains; this is not global A+ approval.

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t46-aggregate.json | a67df1eb1475c06234f7a2199909baddd9ac47bd12cd3bc8a42b30293a09acae |
| t46-all-methods.json | bbce2b392b95c609f378558f079de06c2a20e2328a27cf10d21d6654f0d91b49 |
| t46-method-gaps.json | ed0363dc23f96bc5e8b4b68fd422f3654be060ec765ab24f68e4703568cf9336 |
| t46-branch-only-gaps.json | 62644d09e950987b97a9bc61305ab2990f7fb8270c6a3f606f6aa5bbfaf67c4d |
| t46-profile-progress.json | 673df82241bbfb1b0c7aed122f82cc6947419e5debd27d8e99b7f8aa72cd6515 |
| t46-gap-delta.json | 8a2cda67960c1681d6fc71b9b7820cdd061253ef91f31091145bd3ece8868504 |

## Continuation

The independent audit is complete. Publish this documentation before starting
another implementation packet. Continue with larger coherent behavior packages and one
final product-wide measurement each. The remaining gap union needs behavioral
assessment; delegated wrappers and generated identities are not automatically
critical defects. The all-repository Roslyn API/comment audit remains deferred
until the coverage/CRAP goal is complete. Global A+ remains open.
