# Changelog

Product and release history. This file is not the Apache-2.0 section 4(b) record: that is the
generated [CHANGELIST.md](CHANGELIST.md), which lists every file changed against the upstream
baseline.

## Unreleased

ViciOne.ServiceBus has not been released. The repository is in a private development state, and the
entry below records what the current work changed for anyone reading the source.

### Verification added during the source review

- Fixed RabbitMQ no-ack publishing through shared and scoped channel views:
  linked caller, owner and parent cancellation now remains active until the
  actual client publish finishes, even though the public no-ack call returns
  immediately. Scope disposal waits for all in-flight publishes before
  releasing the parent link. Five red-first cases exposed the original loss;
  expanded tests cover two concurrent publishes, successful cleanup and exact
  awaited failure. The full RabbitMQ unit project passes 505/505 and the
  independent Red Team re-review is PASS. The complete Line/Branch/CRAP audit
  remains scheduled for the larger packet milestone.

- Corrected RabbitMQ duration argument conversion for queue and exchange
  declarations. Whole-millisecond values retain the broker's 32-bit argument
  type where possible and use an exact 64-bit value beyond that range;
  fractional and negative values fail before changing existing arguments.
  Generic `x-expires` durations require a positive value. Queue expiration
  now rejects positive sub-millisecond values, round-trips through its getter,
  and reaches the broker topology without floating-point precision loss or a
  change from 32-bit to 64-bit argument type. Red-first tests reproduced the
  defects; the full RabbitMQ unit project passes 496/496. The complete
  coverage/CRAP measurement remains scheduled for the grouped milestone.

- Fixed RabbitMQ queue configuration boundaries: rejecting an invalid quorum
  replication factor now preserves the earlier classic queue, exclusivity and
  priority settings; fractional-millisecond acknowledgement timeouts no longer
  overwrite a valid broker timeout with a truncated value. Whole-millisecond
  timeouts now use exact integer conversion, fixing a large-value precision
  loss found by Red Team. Red-first endpoint tests reproduced all three errors.
  Focused boundary, stream and requirement tests pass 6/6; the next complete coverage/CRAP audit remains grouped with later
  packets.

- Rejected an empty token returned by a configured one-time scheduling token
  selector before command, delayed-transport or SQL endpoint resolution. Such
  a token could previously be reported as accepted and then rejected by the
  Quartz consumer or SQL cancellation path. Red-first Core tests reproduced
  the acceptance; final focused Core and SQL suites pass 24/24 and 4/4,
  including requirement projections and valid-token recovery.

- Corrected assembly scanning when the same assembly is registered through
  direct, named and type-based entry points. Repeated registration previously
  duplicated discovered types; a red-first public API test observed three
  entries for one assembly. The scanner now shares its existing deduplication
  path. Scanner, finder and cache tests pass 18/18, and the requirement
  projection passes 1/1; the full coverage profile
  is reserved for the next grouped measurement.

- Fixed Event Hubs partial-batch progress: after one provider batch succeeds
  and a later one fails, a retry sends only pending messages. Confirmed
  contexts receive `PostSend`; unresolved contexts receive `SendFault` with
  the original failure. Cleanup and observer failures cannot replay a
  confirmed message. A confirmed message's telemetry span no longer records
  a later message's failure. Red-first tests caught the replay and false span;
  focused sender/producer suites pass 15/15 and 12/12, with a separate 3/3
  real broker delivery control. The subsequent complete 33-profile audit
  passed 13,318 tests but exposed one CRAP>30 hotspot in the new
  batch send method. The method was then split into focused steps. Fresh
  focused coverage reports CRAP 6.04 for the send method and 14.27 for its
  largest helper; the complete frozen measurement predates this extraction.

- Corrected send outcome ownership across the common transport and Event Hubs
  single/batch producers. A failed post-send observer or logger after provider
  confirmation can no longer report a false send fault or make the confirmed
  send retryable. Fault-observer and logger failures no longer replace the
  original provider exception. Red-first tests reproduced both errors before
  the correction; focused Core and Event Hubs outcome tests pass, and the
  complete Core project passes 6,897/6,897. Event Hubs outcome tests use a
  controlled transport context. The existing broker suite passed 96/97 in one
  fresh fixture; the sole checkpoint observation timeout passed 1/1 in
  another. The fixture runner now waits for the Event Hubs emulator's own
  entity-ready signal. A later failed Event Hubs partial batch can still misclassify
  earlier confirmed messages and is tracked as the next batch-progress case.
  T59 remains the latest complete global coverage/CRAP measurement.

- Corrected ActiveMQ producer creation shared by concurrent sends: cancellation
  of the first sender no longer cancels another sender's pending producer.
  The session executor now receives the cache-owned creation token through an
  internal path; the public cache API remains unchanged. A red-first session
  test reproduced the failure, and new cache tests check canceled waiters and
  independent destinations. A three-flavor broker test checks exact native
  routing, identity, priority and durability across sequential sends. Full
  ActiveMQ Unit and LocalIntegration suites pass 229/229 and 106/106 with no
  skips; fresh broker fixture findings are empty. T59 remains the latest
  complete global coverage/CRAP measurement.

- Added six persistent JobService integration cases across Azure Table/Azurite
  and EF Core/PostgreSQL. They check genuine overlapping success and fault,
  per-job terminal state and events, and release of a held execution slot after
  completion, fault or cancellation. The affected local projects pass 44/44
  and 96/96 without skips; both provider fixtures have empty findings. A
  deliberate Completed-state slot-release defect fails the new Azure Table
  completion test. Observation timeouts now match the validated local-provider
  operation timeout. T59 is the latest complete 33-profile coverage/CRAP
  baseline; no new product-wide figure is claimed for this packet. The next
  full measurement will cover several larger connected packets.

- Added a larger multi-response Saga request verification packet: stored
  request-ID routing, three response types, service fault, real Quartz timeout,
  callback owner override, missing/wrong IDs, two active Saga owners and stale
  one-/two-/three-response generations. Thirty new cases and the complete
  Quartz project (318/318) pass. Two deliberate correlation defects were
  detected by the corresponding tests and restored byte-for-byte. The frozen
  33-profile measurement passes 13,287 tests across 32 product assemblies:
  86,006/93,754 physical lines (91.73582%), 31,084/36,845 conservative
  branches (84.36423%) and zero methods with CRAP>30. Independent receipt/XML
  audit and four accepted broker-fixture groups are clean. One initial Azure
  Table local test observation failed and passed in a fresh fixture; the failed
  attempt remains documented. No product source changed; global Line/Branch A+
  remains open.

- Added a connected registration-to-failure verification packet: filtered
  consumer discovery and its definition, scoped compensation with observed
  disposal, retry exhaustion before selected rescue, and message-journal
  ownership across two active buses. Seven new integration cases and the Core
  suite pass; a wrong-owner product mutation fails both journal variants.
  The frozen 33-profile measurement passes 13,257 tests across 32 product
  assemblies: 85,958/93,754 physical lines (91.68462%), 31,057/36,845
  conservative branches (84.29095%) and zero methods with CRAP>30. Four
  broker-fixture groups and independent receipt/XML audit are clean. No
  product source changed in this packet; global Line/Branch A+ remains open.

- Corrected Consumer, Handler and Instance middleware lifecycle accounting:
  a failure in `next` after successful consumption now propagates without
  recording a second fault for the already completed consumer. Its process
  activity and metrics also finish before downstream work. A red-first test
  reproduced the duplicate fault in all three forms. Twenty-eight new cases
  check exact context and exception identity, asynchronous notification order,
  caller versus dependency cancellation, per-delivery retry budgets, circuit
  recovery and process-span ownership. The affected Core suite passes
  6,887/6,887 without skips. The frozen 33-profile measurement passes 13,250
  tests across 32 product assemblies: 85,902/93,754 physical lines (91.62489%),
  31,048/36,845 conservative branches (84.26652%) and zero method CRAP>30.
  Relative to T56, 13 more lines and 12 more conservative branches are covered.
  Global A+ coverage remains open.

- Durable admission atomicity verification: the EF reliable inbox now has
  SQLite and real PostgreSQL regression cases that save a first business record
  and outgoing intent inside a transaction, reject a later oversized message,
  verify rollback and persisted retry, then commit only a distinct replacement
  with its exact serialized body. Direct EF scoped-outbox cases cover the
  caller's explicit Abort and Commit choices after a later rejection. Both
  requirement projections pass; an isolated cleanup counterchange and a
  commit-instead-of-rollback counterchange are detected by the tests. The
  PostgreSQL case exercises the registered EF provider and scoped factory;
  it does not claim broker dispatch. The corrected complete 33-profile run
  passes 13,222 tests with 85,889/93,754 lines (91.61102%),
  31,036/36,845 conservative branches (84.23395%) and zero CRAP>30. This
  packet adds four behavioral cases; the unchanged product source has ten
  fewer observed lines and one more observed branch than T55. Global A+
  remains open.

- Consumer-outbox recovery work in progress: reject a loaded message without a
  DestinationAddress before treating the delivery pass as complete, and pass the
  linked delivery timeout/cancellation token into endpoint resolution. Three
  focused cases reproduce the original failures and pass after correction;
  requirement projection also passes. Thirty-three PostgreSQL cases now verify
  retained corrupt intents, committed delivery windows, allowed at-least-once
  replay, Save/Commit/Cleanup failure recovery and exact neighbor preservation;
  pending-send deadlines and late pipeline failures retain recoverable intent;
  the combined local control passes34/34 including requirement projection. A test
  observer mismatch on typed consume faults was corrected without changing product
  error semantics. Two isolated counterprobes detect neighbor deletion and a missing
  final-batch delivery watermark; product files were byte-restored. The frozen
  33-profile/provider measurement passes13,218 tests without failures or skips;
  line coverage is85,899/93,754 (91.62169%), conservative branch coverage
  31,035/36,845 (84.23124%), with zero method CRAP>30. The remaining5,819
  method gap identities keep global A+ open. Independent read-only audit verifies
  all33 receipts,487 hashes and exact XML-derived aggregate counts.

- Corrected a race in the SQS Quartz integration test discovered by the T54 full
  measurement: consumer delivery can precede Quartz trigger removal. The test now
  observes matching finalization and actual bounded store removal, retains the
  absence assertion, and checks delivery count after bus stop. A blocked-finalization
  counterprobe fails as required; restored controls pass2/2. This corrects test
  synchronization and does not claim a product delivery defect.
- Added a combined transport verification packet for Event Hubs configuration
  ownership and repair, endpoint identity, deferred producer resolution/delivery,
  and native ActiveMQ group isolation across OpenWire, AMQP and Artemis. The
  22 new cases distinguish rejected configuration from successful continuation,
  exact inherited/overridden metadata, provider failure/cancellation and missing
  awaits. Read-only review strengthened cleanup and overload discrimination.
  Both isolated counterprobes are detected and restored; combined controls pass
  27/27 without skips. After the documented SQS test correction, the fresh complete
  measurement passes13,182 tests across33 profiles and four clean fixture groups:
  91.59814% lines,84.21581% conservative branches and zero CRAP>30. Independent
  integrity and numerical audits agree. Global A+ is open with5,826 gap identities;
  no product defect is claimed from this test-only packet.
- Added a connected Azure Table saga-persistence verification packet: 13 cases
  cover corrupted native and serialized rows through public loads and real
  consumption, schema evolution with absent versus explicit empty values, and
  exact UTF-16/binary/UTC storage limits. Rejected inserts and updates perform
  no HTTP writes; existing rows, ETags and neighbors remain intact. Repaired
  sagas continue through the same repository. Independent review strengthens
  the JSON error-category oracle; two isolated counterprobes are detected and
  restored. Final focused controls pass19/19 with zero build warnings/errors.
  No product defect is claimed from this test-only packet. One complete run passes
  13,160 tests across33 profiles and four clean fixture groups:91.55120% lines,
  84.11811% conservative branches and zero CRAP>30. Independent integrity and
  numerical audit agrees. Global A+ remains open with5,837 gap identities.
- Added a connected scheduling verification packet: 32 cases cover publish
  admission before initializer or endpoint effects, control-command resolution
  and delivery ownership, invalid Quartz replacements and provider failures.
  Existing target schedules and same-name neighbors in other groups remain intact
  on rejection; subsequent valid commands preserve exact payloads and headers.
  Read-only review strengthened asynchronous failure and header assertions.
  All three isolated counterprobes are detected and manually restored; final
  focused controls pass179/179 without skips. No product defect is claimed from
  this test-only packet. Its full-product measurement passes13,147 tests across
  all33 profiles and four clean fixture groups:91.54160% lines,84.10997%
  conservative branches, zero CRAP>30. Independent integrity and numerical audit
  agree. Global A+ remains open with5,843 line/branch gap identities.
- Added a connected receive/settlement verification packet across Core, Amazon
  SQS, SQL transport and Azure Service Bus: 39 cases cover exhausted duplicate
  fallbacks, canceled waiters, pending receive work, renewal failure and drain,
  rejected ownership, exact settlement metadata and awaited abandonment.
  Three isolated counterprobes are detected and manually restored; final focused
  controls pass72/72 without skips. This packet changes tests and the SQL test-only
  fake-time dependency; it does not claim a newly fixed product defect. The full
  product measurement passes13,115 tests in33 profiles with four clean fixture
  groups:91.53094% line coverage,84.08554% conservative branch coverage and zero
  methods with CRAP>30. Independent integrity/numerical audit agrees; global A+
  remains open with5,850 line/branch gap identities, including generated methods.
- Corrected raw JSON forwarding through a consumed interface losing the original
  concrete message contract URNs. The preserving serializer now receives the
  nonempty original declared contract set, so a concrete downstream consumer can still
  be selected. When raw header contracts are absent, separately restored send
  contracts are retained, including Quartz scheduled delivery. The combined
  JSON-boundary packet adds value-conversion failures,
  configured envelope admission and independent forwarding targets. Its raw
  regression fails against the original implementation while three forwarding
  controls pass. All three isolated counterprobes are detected and restored;
  56/56 combined corrected controls and 6/6 scheduling controls pass with zero
  build warnings/errors. The corrected product-wide measurement passes 13,076
  executions in all 33 profiles, including 284/284 Quartz cases. It records
  91.51067% line coverage, 84.07197% conservative branch coverage and zero methods
  with CRAP > 30. Global A+ acceptance remains open.

- Added twelve combined Courier journeys for execute retry with route replacement
  or termination, successful and exhausted compensation retries, and activity
  deadlines during execution or compensation. Real received effects distinguish
  committed attempts from discarded attempts; logs, variable removal and failure
  ownership are checked through the complete routing-slip pipeline. Controlled
  pending work and an independent healthy slip prove deadline and outbox
  isolation. Both sequential and concurrent delivery are exercised. Read-only
  review strengthened the negative event assertions. Missing compensation-outbox
  registration and incorrect timeout-token propagation are detected by isolated
  counterprobes. Product source remains unchanged; 69/69 restored controls pass.
  Commit `ad3ddbd40` passes one complete 33-profile measurement with 13,032
  successful executions. Lines reach 85,771/93,753; conservative branches reach
  30,948/36,845. Independent integrity and numerical audit confirms all counts,
  physical changes and gap transitions without discrepancy. Overall A+ remains
  open with 5,855 remaining line/branch-gap identities and zero CRAP > 30.

- Added seventeen combined MultiBus host and scheduler scenarios. Real Generic
  Host lifecycle tests distinguish each bus's health options, failure floor and
  continued delivery when the other bus stops; invalid options identify their
  owner without contaminating the companion. Scoped endpoint, publish and native
  delayed schedulers deliver through the correct bus with their own scope and
  clock. Received recurring/control commands retain exact identities. These tests
  prove command routing, not execution by an external scheduler. Product sources
  are unchanged. Both injected ownership faults are detected, and 50/50 restored
  new/existing controls pass. Commit `3422dc2fd` passes one complete 33-profile
  measurement with 13,020 successful executions. Lines rise to 85,703/93,753;
  conservative branches to 30,937/36,845. Independent integrity and numerical
  audits confirm the deltas and retained branch-only gaps. Global A+ remains open.

- Added ten Saga journey cases across Core and Quartz. Real transport verifies
  owner/event-dependent callback metadata, pending factories and sends, primary
  dispatch failures and failed compensation. Reusing a Saga for a second request
  proves that late responses, faults and timeout messages from the first request
  cannot change the second request or cancel its real Quartz trigger. Both trigger
  existence and removal are observed. Selected callback-pipe and timeout-owner
  counterchanges are detected; restored combined checks pass20/20 Core and22/22
  Quartz, with no product source change. Commit7d0e3d332 passes one complete
  33-profile measurement and13,003 executions. Line coverage remains85,587/93,753;
  conservative branches rise to30,906/36,845. Independent integrity and numerical
  audits find no packet blocker. Observation gains/losses and remaining method
  gaps are explicitly reconciled; global A+ remains open.

- Fixed ActiveMQ receive timestamps being shifted by the local UTC offset when
  the OpenWire SDK returns a local DateTime. The provider now converts the
  timestamp to UTC before checking epoch eligibility and exposing its instant.
  Regression cases use real OpenWire and AMQP message objects, preserve exact
  milliseconds and reject timestamps at or before the epoch. The combined
  transport-header packet also verifies native-map overwrite/removal, RabbitMQ
  reserved metadata and SNS/SQS filtering of normalized scalar values.
  All four selected deliberate faults are detected; restored isolated checks
  pass21/21 ActiveMQ,21/21 RabbitMQ and13/13 SNS/SQS including requirement
  projections. Commit01617f4ec passes all33 product-wide profiles and12,993
  executions without retries:85,587/93,753lines and conservative30,903/36,845
  branches. Independent behavioral and numerical reviews found no concrete
  blocker. Remaining enumeration-wrapper and branch gaps stay documented;
  no global A+ claim is made.

- Added six InMemory reliable-inbox behavior cases for ownership changes during
  consumer failure, delayed consumer commits and admission rejection followed by
  explicit operator recovery. Assertions prove discarded buffered messages,
  preserved current ownership, exact due-time eligibility and corrected content
  under the same outgoing identity. Main Core tests pass 6,737/6,737 and four
  correctly targeted deliberate faults are detected. A misdirected preliminary
  probe is documented separately. The isolated full control exposed a race in
  the new test's global quarantine expectation: nested recovery does not clear
  a failed send registered with the enclosing consumer. Assertions now check
  both consumer states at their completion boundaries. Corrected isolated
  verification passes 15/15 with no review blocker. Commit5c3e6c3a4 passes all33
  profiles and12,953executions:85,536/93,749lines and conservative30,862/36,843
  branches. No product defect is claimed; global A+ remains open.

- Added eighteen EF reliable-store regression cases for composite inbox
  pagination, stale or removed leases, failure atomicity, initialization recovery,
  capacity boundaries and explicit schedule due times. Real SQLite checks preserve
  retained records, neighboring stores and capacity ledgers across failures.
  All five isolated deliberate faults are detected; main and restored isolated
  EF suites pass 340/340. No product defect was reproduced. Commit a30933c1c
  passes all 33 profiles and 12,947 executions without retries. Product totals
  are 85,532/93,749 lines and conservative 30,858/36,843 branches; target store
  gains 37 covered lines. Global A+ remains open.

- Added six EF Core inbox regression cases for stale and removed consumer
  attempts across completion, retry and quarantine. Real SQLite assertions prove
  current-owner fencing, exact persisted state, no resurrection and unchanged
  same-message, same-consumer and other-store neighbors. All five deliberate
  faults are detected; main and restored isolated EF suites pass 322/322.
  Production behavior already passes these cases. Full measurement at a763757b4
  passes 33 profiles and 12,929 executions. The ownership/missing helper reaches
  9/9 lines, 4/4 branches and CRAP 4. Product totals: 85,485/93,749 lines and
  conservative 30,839/36,843 branches. Non-target gaps remain recorded; A+ is open.

- Fixed Azure Functions receiver cache collisions that dispatched messages to the
  previously selected consumer at the same queue or subscription path. Cache keys
  now distinguish transport, dispatch kind and handler type; each pipeline gets
  independent registration state while preserving bus ownership. Consumer-only
  all-handler dispatch no longer requires the optional Saga capability. Fifteen
  behavior cases cover consumers, saga state, activities, invalid subscriptions
  and typed-bus context. All five deliberate faults are detected; restored
  isolated tests pass 395/395. Full measurement at c28e9feb4 passes all 33
  profiles and 12,923 executions: 85,480/93,749 lines and conservative
  30,840/36,843 branches. Changed method identities and generated accessors
  remain separately accounted for; global A+ remains open.

- Added six Cron regression cases for invalid tokens, Unicode whitespace and
  calendar-union ordering across month boundaries, coincident dates and year
  exhaustion. Removed unreachable private parser guards and simplified calendar
  selection with preserved semantics. Three deliberate faults are detected;
  restored isolated Core tests pass 6731/6731 without skips. Full measurement at
  118cc5ded passes 33 profiles and 12,908 executions. Targets reach full line
  coverage and CRAP 20/18; four target branches remain. Product totals are
  85,401/93,751 lines and conservative 30,808/36,823 branches. Removed code and
  changed reachability are separately reconciled; global A+ remains open.

- Added eight RabbitMQ factory-option cases proving independent message/frame
  limits, certificate/provider/static-credential priority and actual cluster
  resolver selection with port/TLS projection. Four deliberate faults are detected;
  restored isolated controls pass 482/482. No broker-authentication or negotiated-
  limit claim. Full measurement at d286a336a confirms 33 profiles and 12,902
  executions; SQL fixture startup failed before tests and was separately retried.
  Factory reaches 46/46 lines, 25/28 branches and CRAP 28. Product totals:
  85,407/93,762 lines and conservative 30,814/36,841 branches. A+ remains open.

- Added 16 DynamoDB registration validation cases covering complete diagnostics,
  rejection before registration/context creation, valid TTL boundaries, immutable
  registered options and lazy context creation through actual DI. Three deliberate
  faults are detected; restored isolated controls pass 44/44 without skips.
  No product code change or cloud persistence claim. Complete measurement at
  49dbda6d9 passes 33 profiles and 12,894 executions; Validate reaches 14/14
  lines and branches, CRAP 14. Product totals: 85,402/93,762 lines and conservative
  30,813/36,841 branches. Non-target observation changes remain documented;
  global A+ is not established.

- Fixed an ActiveMQ Quartz integration-test race: trigger finalization precedes
  job-store removal. The test now observes removal within a bounded deadline while
  retaining exact delivery and broker-state assertions. Both protocols pass;
  deliberately blocking finalization causes both to fail at the deadline, and
  restored isolated controls pass. The first T38 full measurement remains recorded
  as failed; fresh T38b passes33profiles/12878executions at7c617f34c, including
  ActiveMQ100/100. Lines85396/93762, conservative branches30809/36841,
  no CRAP strictly above30; A+ remains unproven.

- Added 44 Azure Service Bus subscription processor cases for message/session
  callbacks, exact arguments/tokens, awaited completion, configuration guards,
  start/stop/close/dispose and documented warning behavior. Four deliberate faults
  are detected; restored controls pass44/44 without skips. No real broker or full
  SDK-processor disposal claim. T38b covers69/69 method lines and41/44 branches
  in SubscriptionClientContext; three branches remain open.

- Added Event Hubs processor lifecycle tests proving cancellation forwarding,
  isolation of equal offsets in distinct partitions, shutdown waiting for an
  in-flight checkpoint, and subsequent client re-leasing. Both deliberate faults
  are detected; restored controls pass 2/2 with no skips. Checkpoint callbacks
  are simulated. All 33 profiles now pass with 12,834 executions; line coverage
  is 91.0507%, conservative branch coverage 83.5699%. ProcessorLockContext.Canceled
  reaches 3/3 lines. There remain 4,466 line-gap identities and 1,508 additional
  branch-only candidates. Unrelated observation changes are not attributed to
  the new tests. A+ remains open.

- Added Event Hubs checkpoint tests for newest accepted offset, ordered fallback,
  healthy continuation after rejected updates, and cancellation of a waiting
  admission in a full queue. Exact callback order, stored offset and tokens are
  checked with real batching and confirmation objects. Three mutations are
  detected; restored controls pass 5/5. Provider storage remains simulated;
  all 33 profiles now pass with 12,832 executions. Line coverage remains
  91.0465%; conservative branch coverage is 83.5754%. BatchCheckpointer's
  measured coverage is unchanged; behavioral evidence is stronger. There are
  still 4,467 line-gap identities and 1,509 additional branch-only candidates.
  A+ remains open.

- Completed T35 across all 33 profiles: 12,827 passing executions, 91.0465%
  line and 83.5699% conservative branch coverage. Both recurring schedulers
  have stronger command/pipe/completion evidence. Coverage increased by 100
  physical lines and 39 conservative branches; method guards remain, and
  three line-gap identities reappeared elsewhere. A+ remains open.

- Added 132 recurring-scheduling cases across endpoint/publish schedulers,
  explicit/publish destinations, message and pipe forms, and delayed success,
  failure or cancellation. Assertions check exact contracts, payloads, schedule
  values, headers, caller/provider tokens and completion semantics. Three
  Publish-scheduler mutations are detected; restored controls pass 132/132.
  The complete product-wide measurement of this packet remains pending.

- Completed T34 across all 33 profiles: 12,695 passing executions, 90.9398%
  line and 83.4641% conservative branch coverage. EF Saga RollbackAsync is
  fully covered at 5/5 lines. Changed observations elsewhere leave net four
  fewer covered lines than T33; their causes remain open. There are 4,464
  line-gap identities and 1,505 additional branch-only candidates. A+ is open.

- Added real-SQLite Saga load/query checks that preserve the original operation
  exception when rollback also fails. Tests prove transaction identity, an
  uncancelable rollback attempt, no commit, owned-context disposal and healthy
  subsequent reads. Three deliberate faults are detected; restored controls
  pass 2/2. The full product-wide measurement of this packet remains pending.
- Completed the T33 measurement across all 33 profiles: 12,693 passing
  executions, 90.9441% line and 83.4641% conservative branch coverage. All 30
  outbox scheduling overload bodies now have full line coverage. Across the
  product, 37 line gaps closed and one reappeared; 4,465 line-gap identities
  and 1,506 branch-only candidates remain. Product-wide A+ is still open.
- Added outbox scheduling lifecycle checks across typed, runtime and initialized
  messages, pipe variants and explicit/input/publish destinations. Tests verify
  exact schedule metadata, real checkpoint rollback, deferred cancellation,
  repeated commit, provider errors and successful recovery without phantom
  cleanup. Focused 78/78 cases pass; three isolated mutations are detected.
  The complete all-profile measurement of this packet remains pending.
- Completed the T32 measurement across all 33 profiles: 12,615 passing
  executions, 90.7852% line and 83.4586% conservative branch coverage. SQS
  ApplyResponse and EF AwaitConsumerCompletionAsync now have complete line and
  observed branch coverage. Five line gaps closed and two reappeared elsewhere;
  4,501 line-gap identities and 1,505 branch-only candidates remain. A+ is open.
- Added SQS checks for absent opposite result collections in all-success and
  all-failure batch responses, and real-SQLite checks for consumer completion
  preceding dispatch transition and rejection of a reclaimed lease's old owner.
  Tests assert exact caller outcomes, persisted bytes, store isolation and
  capacity. Read-only review strengthened ordered byte assertions. Four isolated
  mutations were detected and restored; focused controls pass 6/6 and 15/15.
  The new packet's complete 33-profile measurement remains pending.
- Completed all 33 profiles for the SQS batch packet: 12,611 passing executions,
  90.7788% line and 83.4288% conservative branch coverage. ApplyResponse now
  has 20/20 lines and CRAP18, with two branches still open. Five line gaps
  closed and two newly appeared; no product-wide A+ acceptance is claimed.
- Added SQS batch identity checks for reordered mixed success/failure results
  and atomic rejection of duplicate or contradictory response IDs. Every
  caller's exact outcome, complete request membership and provider token are
  checked. Deliberate wrong-caller and missing-duplicate-guard changes are
  detected; product source is restored. Full packet measurement is pending.
- Completed all 33 profiles for the cancellation packet: 12,607 passing
  executions; all four targeted cancellation methods have full line/observed
  branch coverage. Overall coverage is 90.7628% lines and 83.4179% conservative
  branches. Six newly observed method gaps remain under investigation; covered
  lines decreased by six versus the previous run. A+ remains open.
- Added Event Hubs confirmation cancellation checks for partition/offset
  isolation, replacement admission, exact cancellation tokens, preserved
  terminal outcomes and rejection after processor shutdown. Added a request
  lifecycle check that repeated cancellation during failure cleanup cannot
  replace the original transport or response exception. Both packets detect
  deliberate incorrect terminal-state changes; product-wide measurement of
  this packet remains pending.
- Completed the 33-profile measurement for the five-gap test packet: 12,604
  passing executions, 90.7692% line coverage and 83.3989% conservative branch
  coverage. All five targeted line gaps are closed. Four newly observed
  cancellation-path gaps remain under review; product-wide A+ remains open.
- Added SQS shutdown checks for an in-flight poll that returns an empty result
  successfully after stop is signaled. No further provider poll or warning/error
  is allowed; the test detects a consume-loop fault hidden by successful agent
  completion. Existing cancellation-path controls remain covered.
- Added RabbitMQ channel-owner invalidation checks after an acquired operation
  fails, and KillSwitch checks that late successful completions cannot revive
  paused or terminated endpoints or contaminate the next recovery window.
- Added real-SQLite checks for repeated actions on removed schedules and inbox
  entries. Exact NotFound results, retained neighbor messages and capacity
  accounting are checked across store instance recreation and store identities.
- Added real Courier execution tracing checks for tracking number, processor,
  argument contract and caller/send/receive/process ancestry alongside successful
  routing-slip completion. These additions verify existing product behavior;
  they do not establish a new product-wide coverage result or A+ acceptance.
- Refreshed the complete 33-profile coverage measurement at one source/test
  commit, including every local provider profile and CPU fallback mode:12,597
  passing executions. Line coverage is90.7521%, conservative branch coverage
  is83.3799%;4,512 method identities retain line gaps. Five newly observed gaps
  remain under investigation. The measurement does not declare A+ completion.
- Added outgoing message-data retention checks for send-policy precedence,
  additional retention, unlimited storage and exact duration boundaries.
  Overflow must fail before repository access. Tests detect omitted retention
  addition while preserving fallback and existing provider behavior controls.
- Added real Quartz checks rejecting all three Saga response types and fault
  replies when RequestId is absent. A valid body identity must not silently
  select a Saga; rejection preserves both Sagas and the actual scheduled timeout.
  A controlled third-response fallback to body correlation is detected while
  the other thirteen response/correlation cases remain passing controls.
- Added real Quartz checks for all five Saga request correlation callbacks.
  Each override must select its configured Saga instead of the default owner,
  preserve the other Saga and its trigger, and address cancellation to the
  selected owner. Ignoring the third-response callback is detected independently
  of the unchanged default-correlation cases.
- Added real Quartz-backed Saga-ID request tests for all three response types,
  service faults and timeout dispatch. Misleading body IDs must not redirect
  responses away from the request-header owner. Response/fault cases verify
  removal of the actual scheduled trigger; a controlled wrong-correlation
  change is detected. Documented the cancellation requirement for positive
  Saga request timeouts and the limitation of transport-delayed scheduling.
- Added completed-initializer transform cases across Execute, Compensate,
  Consume and Send, checking preserved/replaced data, context identity and
  downstream success/failure without premature completion.
- Added asynchronous send-transform checks for message replacement, preserved
  identity/address/header metadata, downstream completion and initializer
  failure without forwarding. A controlled wrong-message change is detected
  by both replacement cases while preserve cases remain green.
- Extended asynchronous transform checks with successful downstream completion
  and Consume-context replacement/failure behavior, preserving the tested
  correlation ID and cancellation token. A controlled wrong-context change is
  detected by the replacement-message assertions.
- Added controlled asynchronous activity-transform tests for initialization
  ordering, context/data identity, downstream completion and exact failure
  propagation. They detect a prematurely completed compensation pipeline when
  its downstream task is not awaited.
- Added real Courier routing-slip tests for repository-backed arguments and
  compensation logs, including deterministic expiry before execution and before
  compensation. Assertions preserve exact content/reference/tracking identity
  and distinguish stage-specific data failures from successful effects.
- Added RabbitMQ nested consume-binding regression tests for the exact directed
  exchange tree, settings, sibling parent restoration and rejection before
  declaration when a parent binding is missing. These tests strengthen existing
  behavior without changing production code.
- Added five real-SQLite inbox cases for failure-state persistence/logger faults
  and terminal winners committed after rollback, two in-memory cancellation
  cases after prepared publication, and seven job-schedule admission/boundary
  cases. Exact exception/token, persistence, outgoing-message and schedule-field
  assertions distinguish controlled product counterchanges. These fourteen
  cases strengthen existing behavior; they introduce no production-code change.
- RabbitMQ fixture health checks run as the broker user. An early root diagnostic
  could create an unreadable root-owned Erlang cookie after the entrypoint's
  ownership setup, preventing broker startup. A forced diagnostic-before-start
  probe reproduces the ownership failure and verifies the corrected startup.
- Updated the background-work architecture guard for the observed ActiveMQ stop
  and monitor tasks, and corrected two asynchronous test/helper names together
  with their requirement projection.
- SQL Server provisioning validates existing principal kinds and SQL login identity
  before transferring schema ownership or granting transport permissions. A user
  occupying the role name, a role occupying the user name, or a same-name user
  mapped to another login is rejected. Regression tests reproduced all three
  unintended permission transfers. Real database tests also verify contained users
  without server logins, connection-string credentials, membership repair and
  quoted passwords. Existing user mappings are never silently rewritten.
- Dependency-graph validation now detects self-edges and rechecks all nodes on
  every validation, including after a prior failure or newly added edge. Four
  regressions distinguish real cycles from shared descendants and disconnected
  acyclic edges. Unused internal topological sorting and comparison methods were
  removed after a repository-wide caller check; live message-fabric validation
  remains intact.
- The SIMD Base32 formatter explicitly initializes its input padding. Existing
  independent reference strings detect poisoned padding at output position 24;
  no prior runtime failure from default stack initialization is claimed.
- Corrected the error-transport filter summary to describe sending and pipeline
  continuation; this filter does not itself generate a fault message.
- One-time setup now clears its running state before publishing success, failure
  or cancellation. Immediate eviction or retry can no longer observe a terminal
  result while the setup still rejects a new attempt. The caller retains the
  original attempt's task even when a completion continuation starts another
  attempt immediately. Six controlled reentrancy regressions preserve exception
  identity, cancellation tokens and exactly one healthy follow-up attempt.
- ActiveMQ now registers send-first temporary destinations in the same connection
  cache used by consumers. Previously a lazy reply endpoint could consume queue B
  after the request had advertised an uncached queue A, losing the response despite
  a successful native send. Queue/topic creation is serialized across sessions so
  competing factories cannot leave unowned native destinations. Six regressions
  cover both startup orders and concurrent creation; integration assertions bind
  the request ID and actual native reply destination across send and response.
- Temporary ActiveMQ registrations now separate equal queue and topic names.
  Lookup, deletion and failed-delete restoration retain the requested type, and
  response addressing explicitly selects a queue. The three low-level context
  lookup/deletion methods now require `DestinationType`; provider extension
  implementations must forward it. Four regressions exercise both creation orders
  and cleanup with and without a native deletion failure. Two native-send cases
  additionally verify the actual reply queue address beside a same-name topic;
  both reject a deliberately wrong topic lookup. The package verification script
  now restores its API inventory tool before building it in a clean checkout.
- The ActiveMQ Classic future-delivery test now waits for a separate scheduled
  probe before checking that the original scheduler job was removed. Classic
  6.2.0 dispatches before updating its scheduler index; client receive/stop was
  not a cleanup barrier. The probe enters a later scheduler iteration while the
  original exact delivery and queue-state assertions remain in place. Artemis
  retains its existing queue-executor observation.
- ActiveMQ connection exception listeners now run in an observed lifecycle task.
  Failed or partially successful registration retires the connection through its
  owning agent; removal failures after disposal are contained, including failures
  in diagnostic logging. A failed close keeps the listener available until a
  successful cleanup retry. Deterministic tests verify native operation order,
  resource ownership, failure identity, throwing loggers and null configuration.
  The previous unguarded registration callback was reproduced terminating the
  test process with an unhandled provider exception.
- ActiveMQ temporary-reply integration tests now distinguish handler entry,
  native reply-address inspection, response-send completion and client receipt.
  Handler failures are surfaced directly, with correlation and bounded queue
  diagnostics before teardown; cleanup cannot replace the primary failure.
  The product request deadline is unchanged and the outer watchdog has one
  shared budget. An injected post-send failure verified both protocol paths
  and was removed. The original intermittent request timeout remains unresolved.
- ActiveMQ connection creation now preserves the original cancellation or
  classified provider failure when disposing a failed connection also throws.
  Cleanup and warning-log failures are contained so diagnostic listeners cannot
  replace the retry-relevant cause. Twelve deterministic cases verify acquisition,
  startup, exactly-once cleanup and exception/token identity, including a throwing
  logger. The previous integration-only coverage of these failure paths varied
  between otherwise successful broker runs.
- Message-contract analysis now follows nested collection element contracts
  instead of comparing collection implementation properties such as `Count`.
  Existing nested implicit-array initializers are repaired at every leaf by the
  missing-property code fix, preserving values, element order and dimensions.
  Regressions cover missing, incompatible and complete leaf values, two and three
  collection levels, and scalar-to-collection conversion. Collection recognition
  is shared by analysis and the code fix; synthesizing a completely omitted
  nested collection remains outside this correction.
- EF outbox quarantine listing now preserves the corrupt attempt counter or empty
  message identifier retained by the producer for classified invariant failures.
  Previously those producer-created rows caused the entire listing to throw.
  Regressions exercise production failure handling, persistence and listing with
  a healthy neighboring row. Corruption cases still reject missing identifiers,
  zero counters and invalid classifications without changing stored evidence.
- The SQL Server parallel-publish test now captures missing message identifiers,
  duplicate counts, warning/error logs and a bounded queue snapshot on timeout
  before teardown. An injected timeout verified the diagnostics and was removed;
  the original intermittent timeout's cause remains unresolved.
- Copied-body stream tests now enforce exact declared lengths, complete short
  reads, stream disposal and isolation from source or returned-array mutation.
  Oversized declarations, including `long.MaxValue`, are rejected before the
  source stream opens. Removing the trailing-byte check failed the new
  truncation regression; all 40 admission contract tests pass after restoration.
  Transport-text selection is now a separate helper from copy admission, with
  the original size-check and strict-JSON validation order preserved.
- Durable payload replay now retains the offload evidence that was actually
  admitted when producing another persistence proof. Previously the current
  send context could replace the original proof's flag, either losing valid
  offload evidence or claiming an offload that never occurred. Both directions
  failed a two-generation replay regression before the correction; all 35
  bounded-serializer admission contract tests pass after it. The regression
  also checks the next replay's acceptance or rejection under a stricter
  message-data threshold, exact bytes, body length and envelope binding.
- EF outbox factory tests now reject absent, incomplete and byte-mismatched
  payload-admission evidence before persistence. A matching proof preserves
  the exact envelope and application headers. The proof check is a separate
  factory helper, keeping admission validation distinct from metadata assembly.
  Removing its envelope-match check failed the new mismatch case; restoring
  the check passes all 281 EF unit tests.
- SQL Server timestamp-projection tests now explicitly make their two inserted
  deliveries due before invoking the normal and partitioned fetch procedures.
  Exact timestamp values, UTC offsets and SQL result types remain asserted.
  Returning `datetime2` instead of `datetimeoffset` failed the projection test;
  the restored procedures pass all 69 SQL Server integration tests. This removes
  the test's immediate-readiness assumption, but does not establish the cause of
  the earlier intermittent empty fetch.
- The endpoint-name contention test now starts dedicated workers instead of
  exhausting thread-pool workers behind a synchronous start gate. It always
  releases and joins its workers after failed setup assertions. A counterprobe
  replacing the shared product lock with per-call locks failed with 16 formatter
  calls instead of one; the shared product lock was restored afterward.
- SQL Server schema provisioning now restores any missing transport-role
  database permission when rerun. Previously one remaining permission made the
  migrator skip all grants, leaving a separately provisioned transport account
  unable to create views after `CREATE VIEW` was revoked. A native integration
  test confirms the account is denied before reprovisioning and can create and
  read a view afterward. The check also recognizes permissions granted with
  grant option as already present.
- Azure Service Bus now registers error-queue and dead-letter move sender
  supervisors with the connection lifecycle. A faulted delivery
  previously reached both its error queue and correlated fault consumer, but
  bus shutdown hung because the error transport retained a connection lease.
  The existing local fault-flow test reproduced the timeout before the fix and
  passes afterward; all 30 Azure local integration tests and 336 unit tests
  pass with the corrected lifecycle.
- Durable copied-envelope replay tests now verify that an exact prior
  admission proof preserves the original body length without consulting a
  mutable locator. Changed body bytes, envelope suffix or content type
  invalidate the proof. A tighter current body limit still rejects a
  previously admitted envelope, while a current MessageData threshold uses
  the offload evidence bound to the proof rather than the new replay context.
  Replacing the proven offload flag with the replay context's flag made both
  positive and negative threshold tests fail; the restored code passes.
- Endpoint-scheduler provider tests now verify that an accepted command keeps
  the configured token consistent across the command, send context, scheduling
  header, correlation and returned handle. They reject a dispatch that skips
  its send pipe, a token change after command serialization, and replay of a
  pipe after acceptance. A deliberately corrupted scheduling header failed the
  accepted-command test; the restored implementation passes.
- Nine RabbitMQ test-double methods now carry the required `Async` suffix,
  matching their task-returning contracts and clearing the repository's
  bidirectional asynchronous naming gate.
- RabbitMQ channel leases now dispose their scoped cancellation links when a
  borrowed use ends. The active agent keeps the scope alive through an in-flight
  pipe operation even if the supervisor begins stopping concurrently. A stop
  budget can still cancel the wait and a later stop can finish after the use.
  A new regression test failed before the fix because a canceled owner still
  canceled a released lease. Channel-lease tests also verify that a borrower
  retains its owner, links caller and owner cancellation, rejects a closed broker channel
  with its exact close reason or an actionable fallback, cancels a pending
  acquisition without disposing the owner, and preserves channel-creation
  failures. A deliberately weakened closed-channel check failed both broker
  shutdown variants.
- RabbitMQ now uses one send-context configuration path for channel-bound and
  direct sends. Channel-bound reply tests verify the final broker frame,
  inherited priority and reply address, cancellation token, and rejection of
  a blank reply route before publish. Omitting the inherited AMQP properties
  failed both the new publish test and the existing direct-context test.
- RabbitMQ startup purge now serializes passive queue inspection and purge
  across concurrent channel starts for the same endpoint filter. A second
  channel cannot purge twice or enter the receive pipeline while an earlier
  purge remains in flight, even when its queue snapshot is empty. Failed
  purges remain retryable; canceled waiters leave the owner untouched. The
  pre-fix parallel test reproduced two purge calls for one startup.
- RabbitMQ receive-transport retries now respect the explicit `IsTransient`
  decision on connection failures. A nested stream error can no longer turn a
  permanent outer failure into a retry, and an exclusive queue conflict remains
  terminal even if an outer connection exception claims transience. Host-policy
  tests cover AMQP reply-code boundaries, nested 405 conflicts, authentication,
  configuration, stream drops, and selective pipelining retries. A deliberately
  changed AMQP 300 boundary failed its test.
- RabbitMQ consumer-delivery tests now exercise the broker callback through
  receive dispatch and acknowledgement. They check AMQP metadata and body,
  both acknowledgement modes, pre-canceled and late callbacks, two broker
  failure types, and continued consumption after an ordinary handler failure.
  Disabling the late-delivery guard makes its regression test fail.
- RabbitMQ cleanup tests verify that open channels and connections close before
  disposal, closed clients skip the handshake, and reply codes and cancellation
  tokens reach the client. Close and state-query failures still dispose the
  resource; channel disposal failures are suppressed while connection disposal
  failures retain their original exception. Removing connection disposal makes
  five of the eleven focused cases fail.
- RabbitMQ dead-letter moves now copy the incoming AMQP header table, retaining
  a dictionary's key comparer, before adding move and host headers. The
  received message remains unchanged, and case-insensitive keys cannot split
  into duplicate reason entries.
  Move-transport tests verify the copied body, properties, routing, mandatory
  publish, closed-channel causes, and topology retry after publish failure.
- RabbitMQ consumer-filter tests verify broker start parameters, readiness and
  completion order, startup cancellation, and reuse of a broker-assigned
  consumer tag after a channel restart.
- RabbitMQ exchange-address tests verify that configured delayed and alternate
  arguments reach the exchange declaration, while removed and incorrectly typed
  arguments cannot silently become endpoint routing options.
- RabbitMQ publish-topology tests now verify topic and fanout parent routing,
  exclusion without builder mutation, an alternate-exchange queue route, and
  direct implemented-contract hierarchy bindings using real broker-topology
  snapshots. The full RabbitMQ unit project passes 411 tests.
- Coverage receipts can limit MSBuild restore/build parallelism on constrained
  hosts and reject a restore that exits without the test project's assets file.
  The selected node limit is recorded in the receipt.
- RabbitMQ Durable Sender unit tests now reject ten unsafe destination forms
  before endpoint resolution, including explicit queue names, short addresses,
  and direct reply-to variants. A valid exchange address reaches resolution
  exactly once. Removing the alternate-exchange guard makes the intended test
  fail; the restored implementation passes all 406 RabbitMQ unit tests.
- All thirteen local integration test projects now reference the centrally
  versioned Microsoft CodeCoverage extension directly. Eleven previously
  lacked it, preventing their runs from producing the Cobertura reports
  required by the product-wide coverage receipt gate.
- Copied send-only JSON envelopes now have behavioral admission tests for
  exact whole-envelope body accounting, the one-byte-over-body-limit boundary,
  lossless text after source mutation, and withheld text for malformed JSON
  or invalid UTF-8. A deliberate invalid-text classification mutation failed.
- The native-test architecture inventory now includes the two retained
  coverage receipt tools, so the path-complete verification gate recognizes
  every tracked Python utility under `tools/ci` and `tools/identity`.
- The SQS publish-discovery fixtures now occupy their own source file and
  matching namespace folder, satisfying the native-test source layout gate.
- SQS lifecycle tests now name asynchronous test cases and provider stubs with
  the `Async` suffix required by the repository's bidirectional naming gate.
- Amazon SQS connection creation now has lifecycle tests for the configured
  provider connection, release on supervised handle stop, failure cause
  preservation, provider cancellation passthrough, and stopping before a new
  connection is opened.
- Amazon SQS send-transport creation now has contract tests for queue topology
  lookup based on the destination kind, ownership of the scoped client and
  transport agents, and cancellation before address validation or registration.
  Provider-boundary tests additionally verify that created queue transports
  invoke SQS sends and topic transports invoke SNS publishes with the expected
  destination and serialized body.
- Amazon SQS consumer connection now has a contract test for all three
  subscription gates: endpoint topology setting, connection option, and
  message-level topology setting. The default overload used by ordinary
  handlers and consumers must enable topology configuration. Every variant
  still connects the requested consumer pipe exactly once.
- Amazon SQS topology cleanup now has lifecycle tests for every combination
  of auto-delete topic and queue flags. They verify single registration after
  repeated configuration, exact cleanup selection, and propagation of the
  endpoint stop token to provider deletion calls.
- Amazon SQS move requests now have provider-boundary tests for the admitted
  body, custom string and binary attributes, FIFO identifiers only on FIFO
  destinations, removal of stale transport headers, and fresh move headers.
  A failed provider send must redeclare topology on retry; a successful retry
  must keep it cached.
- Amazon SQS FIFO receiving now has provider-facing tests for numeric sequence
  order, per-group ordering in an interleaved batch, and progress of a second
  group while the first group's dispatch is blocked. Missing or invalid FIFO
  ordering attributes must stop the receiver before dispatch with the
  corresponding diagnostic.
- Amazon SQS receiver startup now has provider-facing regression tests proving
  queue metadata is resolved before polling, valid visibility settings are
  adopted, malformed or missing settings leave the configured value intact,
  the first poll uses the exact request limit and wait time, and shutdown
  cancels the outstanding provider poll.
- Amazon SQS queue creation now has provider-facing regression tests for
  declared attributes and tags, the missing FIFO flag, unsuccessful creation,
  and an attribute-read failure after creation. The recovery test proves that
  a retry finds the existing queue without issuing a second create request.

### Fixed during the source review since 2026-09-06

- Default typed transport headers now return `false` with an empty result from
  both wire-value conversion methods. Previously a default string header was
  accepted as text, while default numeric and Boolean headers could throw for
  their missing key. Valid typed strings retain their exact key and value.
- Amazon SQS auto-delete cleanup now retains one agent registration across a
  failed topology declaration and its retry, even when each attempt has a new
  scoped client context. Previously each retry could add another agent and
  attempt to delete the same queue or topic more than once. Cleanup uses the
  longer-lived client context after an attempt scope ends; a restarted
  endpoint registers a fresh agent for its new lifecycle.
- Amazon SQS moves now reject a request with more than ten message attributes
  before provider submission, naming the destination and actual count. A
  dead-letter move can add a reason to nine custom attributes; ten existing
  custom attributes plus that reason exceed the provider limit and fail with
  a local diagnostic instead of sending an invalid request.
- Amazon SQS queue-policy reconciliation now checks the Allow statement's
  effect, action, resource, SNS service principal and SourceArn condition
  together. Unrelated statements no longer masquerade as send permission or
  contribute extra conditions to a new grant. A matching explicit Deny,
  including wildcard principals and actions, is reported before a policy
  write. Dedicated ArnLike and ArnEquals grants safely collect multiple
  topics in one statement; existing unrestricted grants are left intact.
- Amazon SNS publish-topology discovery and explicit registration now reject
  a missing configurator or type list with named argument errors. An explicit
  list containing a null or invalid message type, and a namespace scan whose
  filter fails, are evaluated before any type is registered. These invalid
  inputs cannot leave a partly configured publish topology.
- Amazon SQS queue metadata now coordinates resolution, durable ownership
  transitions and removal per queue name. A durable queue can no longer start
  a second provider lookup while an evictable lookup for the same name is
  still being created, and a name lookup cannot create an evictable copy while
  durable creation is pending. Caller cancellation releases only that caller's
  wait; it does not release the queue-name gate before the cache-owned
  operation ends. Independent queue names continue to resolve concurrently.
  Evicted queue metadata now rejects late send and delete requests before a
  lazy batch worker can be created. Disposal waits for policy updates already
  admitted, preserving successful provider writes and their local result.
  An SQS client send or delete that resolves an already evicted queue now
  resolves the name once more before batch admission. If eviction closes a
  full batch channel while the entry still waits for admission, that entry
  also receives one safe retry. Provider failures after admission are never
  replayed, and repeated eviction ends after two lookups.
- Pending delivery-work waits now keep their captured tasks when the caller
  cancels the wait. A later `CompletedAsync` call still observes unfinished
  receive, mediator, queue, or job work instead of reporting completion early.
  Successfully awaited or faulted snapshots are removed by their original
  task identities, so work added during the wait is still drained.
- Generic transport-header adapters now reject a null converter at construction
  instead of failing later during a send. Their policy is covered for typed and
  untyped headers, including host and fault-detail filtering. An Amazon SQS
  queue-send regression test also checks the actual provider request: ordinary
  headers, fault input address and fault message survive, while host and fault
  detail headers are omitted.
- Diagnostic duration formatting now preserves the sign and natural units of
  negative values, including `TimeSpan.MinValue`. A negative millisecond no
  longer appears as a large nanosecond count in chart or transport diagnostics.
  The formatter was split into bounded calendar, clock, and submillisecond
  steps so each method remains below the CRAP risk threshold.
- NewId array-batch generation now validates null arrays, negative indices and
  counts, end bounds, and integer-overflow-sized ranges before reading the
  clock or entering the spin lock. Invalid requests leave the target array and
  identifier sequence untouched; an empty segment at the array end remains
  valid. Previously a negative index could fault while the lock was held.
- Typed exception predicates now inspect nested aggregate failures when the
  aggregate itself has the requested type but does not satisfy the predicate.
  They test both each direct inner failure and its root cause; previously a
  direct match with its own inner exception was skipped. This applies equally
  to include and exclude rules, so a matching inner failure can be handled or
  vetoed as configured.
- Transport bus creation now keeps its configuration and fault-notification
  boundary explicit. A DI regression test proves that a bus-instance
  specification failure remains the reported cause even when a creation-fault
  observer also throws; both failures reach the intended paths exactly once.
- Typed and outer consume-output fault observers can no longer replace the
  original consume-dispatch failure with their own exception. Both groups are
  notified even when the typed observer fails, and diagnostic logger failures
  remain secondary to the dispatch failure.
- Output-pipe fault observers can no longer replace the original dispatch
  failure with their own exception. A failed typed observer also no longer
  prevents the outer observer from receiving the dispatch failure. A failing
  diagnostic logger cannot override either guarantee.
- Azure Service Bus host configuration now treats an endpoint-only connection
  string assigned through the public setter the same way as the string
  constructor. It remains compatible with a separate named-key, SAS, or token
  credential in either assignment order; a credential-bearing string still
  rejects a conflicting authentication mode. Both entry points reject partial
  shared-key pairs and mixed shared-key/SAS values before connection creation.
  The setter rejects another namespace or port without changing the existing
  host or credential. Both entry points also reject duplicate or missing
  namespace endpoints and entity-bound connection strings, which cannot back
  a bus host with independent endpoints. Scoped host and entity addresses
  retain a custom port, including schema-free local emulator endpoints accepted
  by the Azure SDK. Explicit credential setters reject null instead of silently
  clearing authentication and falling back to ambient Azure credentials.
  Credentialless emulator and custom-port connection strings now fail before
  their transport settings can be lost; URI-configured custom ports also require
  a credential-bearing emulator connection string or both supplied SDK clients.
  Credential-bearing custom-port strings without effective emulator mode are
  rejected rather than silently connecting to the default endpoint. Factory
  contexts now preserve the configured namespace port in their reported endpoint
  and derived entity input addresses. Caller-supplied SDK clients may use a
  different physical emulator host or port while the configured logical host
  remains the address advertised to endpoints.
- The outbound HTTP boundary test now serializes its process-wide diagnostic
  listener with other global listener tests. Its synthetic positive control
  observes only its own request, while the default bus lifecycle test still
  detects any unexpected outbound HTTP request. This removes a parallel-test
  false failure seen in the complete Unit/Architecture gate.
- Invalid message types now yield no message-contract metadata, even when an
  infrastructure context implements an otherwise eligible base interface.
  `SendContext`, `ConsumeContext`, and `ReceiveContext` can no longer leak
  `PipeContext` into the contract list after their own validation fails. The
  special `JsonObject` contract exception now requires the actual framework
  type; a foreign type with the same namespace and name stays invalid.
- Saga instance wrappers now apply the same exact-runtime-type rule through
  typed and object equality. A derived wrapper can no longer compare equal to
  a base wrapper as a dictionary key while comparing unequal through
  `object.Equals`; distinct wrappers around equal saga states remain equal.
- Amazon SNS topic addresses now project to relative `topic:` URIs that resolve
  to the same entity under a scoped host. A topic name that cannot be represented
  relative to that scope fails explicitly instead of producing a misleading URI.
- Keyed `Bind<TKey, TValue>` values now apply the same exact-runtime-type
  equality rule through both typed and object comparisons. A derived binding
  can no longer compare equal to a base binding in a dictionary while
  comparing unequal through `object.Equals`; same-owner bindings with equal
  values retain their equality and hash behavior.
- Amazon SNS subscription setup now rejects a failed attribute-read response
  for an existing subscription. A broker HTTP error can no longer skip filter
  reconciliation while the stale subscription is recorded as configured and
  its SQS queue policy is updated. Existing subscriptions also reconcile every
  explicitly configured SNS attribute, including redrive policy, rather than
  silently ignoring settings outside the three filter/raw-delivery keys.
- The public Azure Service Bus connection-string endpoint parser now returns
  no endpoint for empty input and rejects null or blank keys with the intended
  argument or format error instead of leaking indexing exceptions or accepting
  a malformed key. It validates trailing connection-string segments before
  returning the first endpoint.
- The Azure Service Bus emulator dead-letter capability test now waits for the
  broker to confirm dead-letter settlement before receiving from the dead-letter
  subqueue. This removes an overlapping emulator cursor race while retaining
  strict completion checks and adding an exact payload assertion.
- RabbitMQ dependency-injection options now preserve normal, empty, mixed, and
  whitespace-containing credentials exactly through host and client-factory
  projection. Explicit empty values therefore remain anonymous instead of
  falling back to `guest/guest`; unspecified values retain the client defaults.
  Registration tests also enforce TLS enablement, strict and relaxed
  certificate policy, client-certificate identity, connection naming, and the
  complete certificate settings projection.
- RabbitMQ connection creation now preserves supervisor cancellation, owner
  registration, primary failures, and cleanup across settings refresh, both
  real client-adapter routes, shutdown subscription, publication, and disposal.
  Closed or concurrently closing connections cannot be published, subscription
  failures cannot leak an unpublished context, and diagnostic failures cannot
  block lifetime completion. Parallel RabbitMQ, SignalR, and Saga tests now
  synchronize on their actual disposal, consumption, and repository-removal
  boundaries instead of racing those asynchronous product transitions.
- Circuit-breaker runtime settings now reject decreasing recovery-delay sequences even when an
  internal caller bypasses the public options API. Runtime validation tests cover complete error
  aggregation, exact scalar boundaries, nonfinite ratios, missing and nonpositive durations,
  equal durations, and inner sequence inversions.
- Timeout activity and consumer fault handling now use the active delivery context as the
  cancellation authority, reclassify only cancellation from the elapsed configured timeout,
  validate required inputs before honoring caller cancellation, and fully own fault generation and
  receive notification. Consumer timeout wrappers publish through their original delivery context
  while preserving the passed context for receive notification; foreign contexts remain
  authoritative for both cancellation and publication. This prevents unrelated cancellation from
  being reported as a timeout, prevents an owner token from suppressing or canceling the wrong
  fault, and preserves exact asynchronous failures and cancellation tokens.
- RabbitMQ sends now round positive sub-millisecond message lifetimes and delayed-delivery
  intervals up to the next wire millisecond. This prevents a positive lifetime or delay from
  becoming zero and avoids shortening fractional intervals. Send tests also cover direct-reply
  routing, telemetry tags, durable destination validation, cancellation, and mandatory routing
  supplied by a distinct publish payload.
- Reliable-messaging configuration now rejects nonfinite retry jitter before the typed sender
  starts. The invalid-policy tests use a valid baseline and verify the rejected property's name;
  inclusive jitter limits and the frozen delivery policy have their own regression tests.
  Durable retry timing now preserves both outcomes even in a two-tick jitter window, avoids
  numeric wraparound at the largest `TimeSpan`, and retains a failed delivery with a saturated
  due date when adding its calculated delay to the current time would overflow.
- SQL topology subscriptions now compare their nested queue and topic declarations by logical
  broker identity instead of object reference, with matching hash codes. SQL topology diagnostics
  now expose the queue delivery limit, and the public publish-topology registration extensions
  reject missing configurators, missing message-type collections, and null collection entries at
  their API boundary before registering any preceding type.
- SQL host addresses now retain complete password suffixes and reject invalid mutable address
  components before startup. PostgreSQL host lists validate every segment atomically, preserve IPv6
  and inline-port semantics, reject unrepresentable Unix sockets, and rebuild data sources from the
  current configurator values without discarding unrelated security options. Explicit inline
  default ports override competing global ports, and an explicit single-host override clears prior
  multi-host state.
- SQL receivers now retain ownership of fetched delivery locks through shutdown, release late
  batches even when an earlier unlock fails, and wake promptly when a delivery completes during
  queue maintenance. Empty polling is bounded by auto-delete keepalive. Both SQL providers round
  fractional idle lifetimes up to database seconds, so sub-second settings cannot become immediate
  deletion, and address and endpoint validation reject values beyond the SQL seconds range.
- ActiveMQ header projection now preserves both Boolean values, limits native values to the shared
  OpenWire/AMQP message-property set, omits OpenWire-incompatible byte arrays, formats other
  `IFormattable` values with invariant culture, and keeps every `DateTime` kind on the same instant.
  Direct topic-consumer diagnostics no longer emit an empty destination field. Source-owned tests
  cover header precedence, actual OpenWire marshalling and broker round-trips, foreign null-valued
  header implementations, scheduled-delay consumption, transport-property round-trips, topology
  lifecycle identity and runtime message destinations.
- RabbitMQ durable-send acceptance now requires a broker-confirmed, persistent, mandatory publish
  to an existing durable quorum queue (`6154ec2b4`). Queue proof no longer changes broker routing,
  synchronous and asynchronous publish failures both invalidate cached topology, and acceptance now
  follows matching pre-publish and post-confirm queue checks. Concurrent failed sends retain their
  original broker causes while a newer shared topology generation is being rebuilt.
- Saga removal and nested request outcome forwarding were corrected (`6a43c31ed`); in-memory saga
  indexes and queries now preserve registered identities and consistent snapshots (`fd11887df`).
- Semantic asynchronous API names and processor-lease handling were corrected (`7e5095b5a`).
- Reliable inbox operations and their evidence became deterministic (`f38685b51`), and SQL transport
  delivery invariants were enforced (`cbeb76206`).
- Mediator receive contexts no longer share mutable MIME state (`d071332b7`); resource caches release
  constructor-owned state when initialization fails (`020c146f8`).
- MessagePack formatter caches no longer retain the wrong lifetime (`e0d5fc1b0`), and typed inline
  object message data is preserved on round-trip (`bac2c88f9`).
- Payload-admission checks now cover transport and durable replay boundaries (`194271bbd`).
- Container saga test harnesses now resolve when a persistence provider supplies load but no query
  capability; unsupported operations still report their capability error (`468ba2369`).
- EventHubs raw-message tests now assert the actual receive address and default content type, and
  isolate observer events by message identity (`a2e9995a3`).
- The PostgreSQL transactional-outbox test now observes an uncommitted transaction across a polling
  interval and checks delivery after commit (`0578d6830`).
- The EF Future PostgreSQL fixture now retries complete serializable transactions when concurrent
  branches encounter a transient serialization conflict (`7a933284d`).
- RabbitMQ host and receive addresses now reflect TLS changes made before build, then retain the
  built runtime addresses and reject later host-address changes (`17f5dc4bd`).
- Amazon S3 message data now keeps caller streams open, uploads from their current position,
  applies its lifecycle rule only to explicit-TTL objects, rejects legacy untagged rules and
  versioned buckets, and revalidates startup state (`becc51c51`).
- Azure Blob message data now keeps SAS credentials out of new claim-check addresses and logs,
  reads older signed addresses using current credentials, isolates compressed upload block IDs,
  and treats `TimeSpan.MaxValue` as unbounded (`04d9708a2`). Azure TTL enforcement remains open.
- Azure Service Bus topology declarations now snapshot subscription options and rules, reject
  conflicting broker identities across relationship kinds and case variants, bind forwarding to
  the declared destination, propagate partitioning through earlier relationships, and reject
  session-enabled autoforwarding before deployment.
- Azure Service Bus receive headers now take the broker enqueue instant for `TransportSentTime`
  even when an application property uses that name with different casing. Header enumeration
  excludes those forged time entries; other application names remain exact, and raw identity
  headers retain their canonical GUID format. Regression tests also verify persisted routing
  metadata, UTF-8 values, blank values, and session/partition consistency.
- Azure Service Bus receive transport properties now retain the broker's `ReplyTo` destination
  alongside session, partition, reply-session, and label values. Scheduling or replay that
  persists these properties no longer drops a reply destination; a forged application property
  cannot replace the broker value.
- Payload admission now rejects a send-context proxy before attaching an operation marker to its
  underlying transport context, so a rejected proxy cannot contaminate a later send. Event Hubs
  rechecks admission after send observers run, for both single messages and every batch member;
  observer changes to the serializer's content type are rejected before provider submission.
- Event Hubs receive headers now reject a missing SDK event at construction and treat blank
  message and correlation IDs consistently in enumeration and lookup. A lookup for a blank ID
  returns no value; non-null application properties retain their exact names and values.
- Azure Service Bus host retries and reliable-send classification now inspect complete exception
  trees, including every aggregate sibling, and give permanent causes priority. Both paths share
  broker-reason decisions: recoverable timeouts and explicitly transient general errors retry;
  non-transient SDK failures outside entity recovery and permanent HTTP statuses stop. HTTP status 0 remains retryable,
  missing broker entities retain send-side recovery, and the Azure classifier leaves generic
  connection failures to the transport that raised them.
- In applications that register Azure Service Bus before ActiveMQ, Azure's reliable-send classifier
  now delegates failures marked by a foreign transport connection type. This prevents a nested
  timeout or HTTP status from turning an ActiveMQ configuration failure into a durable retry.
  Azure's own retry-stop connection wrapper remains classifiable; tests exercise the actual retry
  wrapper and verify immediate InMemory outbox quarantine with both transports registered.
- Azure Service Bus subscription setup now propagates a missing configured rule on an existing
  subscription instead of reporting success with a broad `$Default` rule. A generated filter on an
  existing subscription requires one identifiable generated rule; ambiguous or externally named
  rule sets fail closed. When another creator wins a subscription race, the winner's delivery and
  forwarding settings and configured rule are reconciled before setup succeeds. Real emulator
  regressions check the persisted rule set, and a controlled SDK race checks both updates.
- Azure Service Bus publish-topology validation now reports invalid composed topic paths and idle
  lifetimes while ignoring excluded topics. Topic settings freeze once evaluated for a subscription
  or broker declaration, so later changes cannot make sender and broker options disagree. The public
  `CreateTopicOptions` getter now returns a separate snapshot: mutating it no longer configures the
  published topic. Use the publish configurator before evaluation; custom broker declarations can
  supply SDK options directly through the provider topology builder. The Azure emulator test
  project now includes its missing Microsoft CodeCoverage extension, so instrumented provider runs
  execute the tests instead of reporting zero discovered tests.
- SignalR's source and test NuGet lockfiles now reflect the earlier removal of their obsolete
  Initializers project dependency. A locked restore succeeds with the current project graph.
- Assembly directory scans now resolve candidates from their selected files instead of binding
  first by the file's simple name, which could substitute an unrelated already loaded assembly.
  Invalid images remain skippable; other load failures report the error from the selected path.
  Tests cover renamed assemblies, filename collisions, recursive filters, executable inclusion,
  disappearing files, and calling-assembly discovery.
- EF JSON change tracking now compares and snapshots the value actually persisted. Selective
  `IEquatable<T>` implementations and shallow `ICloneable` snapshots can no longer silently drop
  changes to serialized fields; SQLite regression tests cover both cases.
- EF saga repositories now isolate EF model-cache entries by repository configuration and rebuild
  the model after a later saga-map registration. Repository probes return contexts through the
  configured factory release path instead of disposing factory-owned contexts directly.
- Raw Entity Framework outbox replay now restores persisted correlation, conversation, and request
  identities together with user headers.
- Direct Amazon SQS factories now require and enforce explicit body and transport-envelope limits.
  The configured JSON depth remains mandatory after later serializer-option callbacks, child
  endpoint overrides, and serialization resets.
- Amazon SQS/SNS message names now reject every open generic shape and distinguish contracts whose
  namespace or type identifiers would otherwise collapse onto the same topic name. Type-based
  destinations use the actual publish topology, repeated scoped host settings do not stack prefixes,
  and the factory rejects a scoped host configured after message or publish topology was created.
  Empty or duplicate naming separators are rejected, and long canonical names use a distinct,
  stable digest form within the SNS topic-name limit. Scoped topics remain within that limit too;
  opaque custom message-topology configurators reject scoped host settings because their cached
  names cannot be checked for consistency.
- Amazon SQS/SNS topology now rejects two declarations of the same queue or topic when their
  broker attributes, subscription attributes, or tags differ. Previously, the second declaration
  could silently reuse the first handle and lose its settings, including an explicit SNS raw-delivery
  choice. AWS names are unique even when an extension supplies a queue or topic entity subclass;
  conflicting subtype declarations can no longer produce duplicate broker names. Diagnostic
  descriptions and both equivalent and conflicting metadata declarations have source-owned tests.
- Amazon SQS/SNS subscription declarations now compare the complete topic and queue definitions
  rather than their object references. Extension collections reuse independently created equivalent
  declarations, reject conflicting definitions for the same broker pair, and prevent duplicate
  pairs supplied through a subscription subclass. The normal builder still reuses a repeated
  topic-to-queue subscription handle.
- Amazon SQS endpoint validation now rejects directly mutated visibility timeouts outside the AWS
  range, maximum visibility durations outside the positive 12-hour range, and renewal intervals
  below the effective 60-second floor. Diagnostics identify the setting that is invalid. The
  existing concurrency, polling, purge, redrive, and SNS raw-delivery checks retain their order;
  regression tests also protect the raw-delivery attribute's string-only boundary.

### Removed

- The inherited message-audit contracts, observers, configuration and provider implementations.
  Their useful diagnostic capture capability is superseded by the intentionally incompatible,
  policy-controlled `MessageJournal`; no audit compatibility alias remains.
- The Python policy validator, its policy modules, and its validator self-test suite. They were a
  discarded Team 1 detour rather than imported behavior. Independently valid safeguards move to their
  effective MSBuild or native xUnit/MTP boundary; the validator must not be rebuilt.
- The foreign licence check and the usage telemetry that reported host, bus, rider and endpoint data
  to a hard wired third party address on every bus start, together with their dependency injection
  and public API surface.
- The inert `TypeAttributes.Serializable` flag on the dynamically emitted message proxy and its
  `SYSLIB0050` suppression. The modern serializers are unaffected.

### Changed

- Message-limit tests now exercise inclusive body and envelope boundaries,
  independently optional warning and offload thresholds, and exact failure
  fields and reasons for invalid settings. A targeted `>` to `>=` mutation of
  the optional upper bound failed the intended two valid-boundary cases; the
  product implementation was restored and the normal suite passed.
- Mermaid label encoding now keeps the syntax-entity table separate from the
  Unicode and control-character loop. Exact syntax, control, and surrogate
  output remains covered by the visualizer behavior tests.
- Job-attempt state-machine setup now separates event correlation registration
  from schedule and state behavior registration while preserving their order.
  The existing transition tests still verify startup, liveness escalation,
  cancellation, fault handling, and finalization.
- EF-Core reliable inbox delivery now keeps lease acquisition separate from
  the existing commit and failure-handling path. A quarantined delivery is
  explicitly tested against duplicate dispatch: its consumer is not invoked
  and its attempt count, failure details, and lease state remain unchanged.
- Typed Durable Sender now resolves and validates its send context separately
  from constructing the serialized intent. The redundant transport-context
  check after the canonical `MessageSendContext` check is removed. New tests
  verify that endpoints without transport capability and noncanonical send
  contexts fail before durable admission, while preserving destination and
  cancellation-token propagation.
- Durable-send intent validation now keeps destination and media-type checks
  together in a focused operation. Validation order remains unchanged. Existing
  boundary tests cover the exception parameters, exact length limits, and
  acceptance of empty serialized bodies.
- Mermaid state-machine label encoding now handles Unicode scalars and control
  characters in a focused helper. The exact generated documents and syntax
  escaping remain unchanged; the encoding tests still cover reserved Mermaid
  characters, control characters, and paired or unpaired surrogates.
- Removed the unused internal Azure Service Bus `ReadAsBytes` stream helper.
  It had no source or test call site and was not an externally accessible
  transport API; keeping its inconsistent stream-position and generic-error
  behavior would create a false maintenance contract.
- Saga-index registration now keeps captured-key publication and rollback in
  one focused operation. The admission lock, exception order, and cleanup
  behavior are preserved while the public `Add` path is simpler to review.
  Transaction tests assert reverse rollback, continued cleanup after failures,
  and the exact primary and cleanup exceptions.
- Output-pipe and consume-output fault notification now run in focused methods
  so dispatch control flow and secondary observer failures are independently
  reviewable. New consume-output tests verify asynchronous callback order,
  pending pipeline state, exact context and exception identity, and failures
  from either observer group and the diagnostic logger.
- Filter-observer tests now hold each typed and untyped pre-send, post-send,
  and fault callback asynchronously. They verify that downstream work waits
  for the active observer, callbacks stay ordered, and a failed dispatch
  retains its original exception even if either fault observer fails.
- Azure Service Bus session-batching tests now exercise the public consumer
  extension through real batch options and the endpoint callback. They verify
  that broker session identities group messages, batch limits reach the
  queue and subscription endpoints, existing prefetch settings follow the
  configured boundary, and invalid options or endpoint kinds fail before
  endpoint mutation. The invalid-endpoint diagnostic now names the actual
  shared Azure Service Bus endpoint contract.
- Consume-observer converter tests now verify that each lifecycle stage
  forwards the exact typed context and fault, preserves the observer's
  asynchronous result and failure even when the caller cancels after
  notification, and rejects missing, wrong-type, or pre-canceled inputs
  before notifying an observer.
- Recurring scheduler tests now verify that both endpoint-backed and
  publish-backed schedulers initialize a scheduled message from supplied
  values and preserve its fields, destination, schedule, cancellation token,
  and returned handle. They also execute the typed pipe adapter and verify
  that the supplied pipe sees the initialized payload and its correlation
  write reaches the outgoing schedule command context.
- Endpoint QoS validation now resolves each endpoint in a focused helper while
  preserving declaration order, canonical values, and aggregated diagnostics.
  A new regression test verifies that endpoint-owned and consumer-owned QoS
  for one dedicated endpoint must agree.
- Recurring-publish scheduler tests now verify that both endpoint-backed and
  publish-backed schedulers resolve the runtime message type to the correct
  destination, preserve the schedule and payload in the command, forward the
  cancellation token, and return a matching handle. They also verify that a
  declared message contract selects its own publish address and that the
  caller's send pipe reaches the command endpoint unchanged.
- Amazon SQS host tests now construct the standard SQS and SNS SDK clients
  without sending requests. They verify that both use the configured host
  region when explicit credentials are supplied and that distinct
  caller-supplied SDK service endpoints and signing regions reach the correct
  client.
- The EF Core reliable-inbox regression suite now verifies cancellation after
  business data and the consumed fence have been flushed inside a transaction.
  The cancellation must roll back both records, clear the scoped change
  tracker, and permit the same delivery identity to commit on its first
  subsequent attempt.
- The typed Durable Sender regression suite now verifies that a relative
  destination is rejected without reserving its idempotency key or writing an
  outbox record. A retry with the same key and an absolute destination must
  persist exactly one intent.
- Request-rate construction now calculates the rounded-up request limit
  without overflowing and caps the default concurrent result capacity at the
  largest supported integer. Waiting for result capacity now responds to
  caller cancellation and disposal, releases the abandoned request lease,
  and returns an unused rate permit only within its original rate window.
  Boundary tests verify maximum prefetch size, cancellation, disposal, and
  rate-window rollover without issuing large result batches.
- Retry execution now keeps preparation of a scheduled attempt in a separate
  method. Contract tests verify that caller cancellation during the operation
  and independent caller or policy cancellation during pre-retry stop further
  attempts and report the originating token; concurrent cancellation retains
  caller precedence.
- Serialization validation now checks serializer and deserializer registrations
  in separate methods while preserving the existing failure order and member
  names. Contract tests cover an empty registration, ambiguous multi-format
  selections, and successful collection creation after explicit selection.
- Amazon SQS topology diagnostics now have a contract test that verifies all
  declared topics and queues, their lifetime flags, and every SNS-to-SQS
  subscription pair in the public probe result.
- Azure Service Bus queue and subscription fault-notification tests now verify
  that processor callbacks return while supervised shutdown is pending,
  overlapping and simultaneous fault reports share one stop attempt, and an already canceled
  notification leaves the supervisor running until a later active fault. A
  failed stop is logged and permits a new attempt on a subsequent fault.
- Two RabbitMQ shutdown lifecycle tests now wait until their connection fake
  has actually removed the shutdown handler before asserting that none remains.
  The previous signal fired before removal and could fail under parallel test load.
- NewId value tests now verify all four identity words through public equality
  operators, typed and boxed equality, dictionary lookup, hash consistency,
  and boxed comparison boundaries. The implementation is unchanged.
- NewId format tests now compare standard and sequential B/D/N/P output,
  case variants, defaults, and invalid format boundaries against independent
  Guid values. The formatting implementation is unchanged.
- JobService correlation and SQL partition-key registration now use smaller
  domain-grouped methods without changing the 30 correlation identities, 31
  partition formatters, or registration order. Source-owned tests verify every
  correlation identity, empty and null boundaries, and repeated registration
  after the global topology is frozen.
- `JobSagaDefinition` now groups its 18 receive partition registrations by
  admission, attempt, lifecycle, and progress/timer messages. A registration
  test verifies the exact 18 message types, one shared partition coordinator,
  and the endpoint concurrency limit. The separate direct-endpoint test verifies
  same-job serialization across message types while another partition continues.
- Azure Service Bus connection creation now separates custom-port validation,
  SDK option setup, and the four explicit credential routes from the namespace
  context. Custom-port rejection checks now include named-key and SAS settings.
  Emulator regressions verify both mixed-client directions with real queue
  administration and exact message delivery: the factory fills in the missing
  client while continuing to use the caller-supplied client.
- Azure Service Bus connection-string endpoint parsing now separates segment
  validation from URI normalization. Direct public-parser tests reject a second
  endpoint even when its key uses different casing, reject malformed segments
  before or after a valid endpoint, and preserve a schemaless scoped endpoint
  through leading, repeated, and trailing delimiters.
- Recurring publish scheduling now has a source-owned regression for the
  declared runtime message contract: the published command retains the
  destination, schedule, payload and contract identity, while the chosen pipe
  and cancellation token reach the publish endpoint. A failed publication
  propagates to the caller instead of returning a scheduling handle.
- The source-owned consume-transform pipeline test now checks the public
  delegate-based property transformation with both present and null source
  values. It verifies that the delegate sees the original message and
  property value even when another property is changed in the same transform.
- Existing Amazon SNS subscription tests now verify the exact broker update
  request and failure propagation for changed or missing filter policy,
  changed filter scope, raw-delivery settings, and redrive policy. Matching
  existing settings avoid redundant SNS updates while queue permission is still
  configured. A failed update cannot be reported as a completed queue subscription.
- RabbitMQ endpoint query parsing now groups lifetime, exchange, Boolean, and entity-name
  options in focused parsing steps. The public address behavior remains the same; source-owned
  regressions check combined host and endpoint options, unsupported schemes, conflicting or
  duplicated options, and stable deduplication of exchange bindings. An unreachable virtual-host
  fallback was removed after confirming the path parser always assigns a value.
- Generated Amazon SNS topic names containing separator characters in CLR identifiers now use a
  reserved canonical encoding, so their durable topic names differ from earlier development builds.
  Existing topics are not renamed automatically: migrate subscriptions and coordinate publisher and
  consumer rollout before using those earlier builds with this version. Configure a scoped Amazon
  SQS host before any message or publish topology to apply its prefix.
- The thirteen direct `ViciOne.ServiceBus.*` sibling projects retain their assembly boundaries, with
  Abstractions documented as the mandatory foundation. Courier, Future, and Saga implementations
  that were owned only by those optional capabilities have moved out of Abstractions into their
  respective assemblies without changing their namespaces or retry behavior. Core now consumes the
  neutral retry-classification contract instead of naming Saga exceptions, and SignalR no longer
  references the optional Initializers project or package; an isolated package consumer and NuGet
  metadata gate enforce that boundary. The API guide records every sibling project's use,
  dependency direction, and selection point, plus the planned provider-specific Saga adapters for
  Azure Service Bus and Event Hubs.
- MessagePack serialization now has symmetric bus and receive-endpoint configuration, isolated
  forwarding snapshots, payload-admission-safe byte handling, normalized byte, Base64 and object
  payload overlays, and System.Text.Json-equivalent case-insensitive recursive overlay semantics.
  Its internal runtime, forwarding serializer, formatter invokers, serializer context, files,
  comments, and source-owned xUnit/MTP contract suite were aligned around their actual
  responsibilities without adding a compatibility shim or test-only product instrumentation.
- Repository builds now follow the current stable .NET 10 patch channel instead of pinning one SDK or
  runtime patch. All direct dependencies and lock files were reassessed and refreshed; Quartz 4 is
  adopted through its `ValueTask` job lifecycle and builder-owned job factory. The former
  `QuartzSchedulerOptions.CreateJobFactory` hook is removed: standalone schedulers now use the
  default ViciOne job factory, while container hosts register scheduling through
  `AddQuartzConsumers`.
- RabbitMQ now owns a Durable Sender dispatcher whose acceptance boundary requires persistent,
  mandatory publishing and a publisher confirmation from a real broker. Unroutable and canceled
  attempts never report acceptance. All other external transports remain explicitly unsupported
  until they can prove an equally strong provider-owned boundary.
- Host lifecycle options validate at startup, and the public surface is documented and enforced as
  Application, Advanced SPI, Provider, Operations, and Testing APIs. Advanced definition, binder,
  manual scheduler, and persistence/dispatcher shapes remain available only where they carry an
  active extension capability and are hidden from default IntelliSense.
- The ambiguous inherited `ITransactionalBus` surface is replaced by two explicit Greenfield
  capabilities. `IAmbientTransactionBus` follows `Transaction.Current` and has no manual flush,
  while `IBufferedBus` exposes an explicit FIFO `FlushAsync` boundary. Both implementations are
  internal, retain every publish/send overload, preserve exact cancellation and failure identity,
  and reject composition with the durable Entity Framework bus outbox because all three own the
  same scoped publish/send boundary. A recursive flush from an action in the same logical drain is
  rejected immediately instead of self-deadlocking; unrelated concurrent callers remain serialized.
  Typed multi-bus registration preserves the original configuration failure across its reflection
  boundary. The former transactional-bus API has no compatibility shim.
- Quartz scheduled-message execution now propagates a causally requested job cancellation instead
  of converting it into up to five immediate refires. A dependency-thrown cancellation remains a
  retryable job failure when the Quartz execution token was not requested.
- Core pipeline behavior now has source-owned native xUnit/MTP coverage for dynamic consumer and
  handler connections, observer composition, context filtering, cancellation causality both before
  and inside an active retry attempt, consumer,
  send and publish configuration layering, partition conventions, and transaction ownership. The
  thirteen inherited NUnit pipeline files are removed after one-to-one disposition of all 34
  inherited obligations. Public handler, context-filter and transaction boundaries fail fast;
  transaction scopes enable asynchronous flow by default, externally supplied transactions retain
  ownership, and every retry receives a fresh owned transaction context. The concrete
  `SystemTransactionContext` adapter is now internal; the public capability remains the neutral
  `TransactionContext` contract.
- RabbitMQ host and endpoint addresses are immutable value objects with strict option parsing,
  scheme-owned TLS semantics, canonical port and URI rendering, defensive binding ownership and
  UTF-8 byte-accurate entity limits. Credentials retain password suffixes after the first colon;
  TLS lets the operating system negotiate enabled protocols and validates certificate chains and
  names by default. Query values and encoded short names round-trip without truncation, queue TTL is
  emitted as a numeric AMQP argument, and destination topology uses the final configured broker
  rather than its constructor default. The complete inherited address fixture is replaced by a
  source-mirrored native xUnit/MTP cohort with a one-to-one disposition of all 46 inherited
  obligations.
- `MessageJournal` is an optional, default-off diagnostic capability for terminal send, publish and
  consume outcomes. A mandatory caller policy selects and sanitizes the serialized envelope before
  an immutable entry reaches EF Core or Azure Table. Both stores enforce finite size, count and age
  on each append; failures are deadline-bounded and isolated from message flow. The feature has no
  query API, background queue, retry carrier, second outbox, log ownership or Suite-audit role.
- The circuit breaker now has one validated greenfield options boundary, an immutable runtime
  snapshot and a timer-free state machine. The snapshot is isolated from retained configuration
  builders and caller-owned arrays and is produced by the shared exception-filter semantics used by
  retry, rescue, redelivery and kill-switch paths. Exactly one caller owns each half-open recovery probe;
  competitors fail immediately with `CircuitBreakerOpenException`. Inclusive throughput/ratio
  boundaries, bounded backoff, causal cancellation classification and no-throw, low-cardinality
  OpenTelemetry signals replace public runtime states, router events and timer ownership.
- Two roots, and a run owns its own child of each. Compilation output under `artifacts/sdk`, packages
  under `artifacts/packages`; the raw TRX, the endpoint projection, the control files and the broker
  logs of one run under `artifacts/run-output/<run>/`; and the durable category record under the
  caller's own evidence parent, in its own `<run>` child. Saying that every file a run writes lives
  below the run-output root was false: the record is the one file meant to outlive the run, which is
  why it is written where the caller asked for it. Two runs on one machine still share no file.
- `.slnx` is the canonical solution format. Product and engineering have named targets; native test
  profiles are additional named targets and are materialized only when they contain an executable
  cohort. The current Unit profile uses xUnit 4 on Microsoft Testing Platform 2. Empty profile
  solutions are forbidden.
- Test support code is framework-neutral under `ViciOne.ServiceBus.Tests.Infrastructure`; test-only
  package versions do not participate in product evaluation, and the inherited NUnit/VSTest/Python
  stack is transition evidence rather than the target test architecture.
- Product Release builds keep embedded symbols while native MTP test applications use portable PDBs,
  as required for xUnit/MTP discovery. Applying the product symbol policy to the test executable had
  produced a successful build followed by a zero-test MTP run.
- Native xUnit executables set `UseMicrosoftTestingPlatformRunner=true`; the hybrid in-process entry
  point is not supported. The MTP-only `testconfig.json` replaces `xunit.runner.json`, fails skips and
  warnings, and CI rejects discovery below the current native profile floor.
- `Directory.Build.targets` carries the late half of the build contract: eleven errors that refuse a
  project which drops its lock file or locked mode, packs without its licence or notice, targets a
  framework this product does not support, or reaches for `netstandard2.0` while being neither a
  Roslyn component nor the analyzer package project whose framework group decides which consumers may
  reference it. The same contract prevents projects outside `tests2/` from claiming its package
  boundary or referencing its native xUnit/MTP entry package.
- The inherited verification inventory was consolidated during takeover. Its remaining runners are
  migration evidence only and are replaced cohort by cohort by the native xUnit/MTP test estate.
- The ActiveMQ publish topology is deployed to the broker. Resolving a destination name is a client
  side act and left the broker without the topic; `SessionContext.EnsureTopicExists` makes the broker
  hold it.
- Cron expressions tolerate repeated spaces and tabs between fields without shifting subsequent
  values into the wrong fields.
- Endpoint-name formatter behavior now has native source-owner xUnit/MTP coverage for snake-case
  boundaries, namespaces, prefixes, generic consumers, instance identifiers, and reserved names;
  the fully replaced inherited NUnit fixture was removed.
- Runtime `MessageUrn` overloads now share one fail-closed input validation path for null and open
  generic types. Native source-owner tests replace the complete inherited URN fixture and add the
  previously missing deconstruction contract without preserving static-cache exception wrappers.
- Request-rate behavior now has deterministic source-owner tests for processing, grouped execution,
  adaptive concurrency, limits, empty results, and invalid options. The replacement removes the
  inherited assertion-free, random, and delay-based fixture.
- Analyzers and code fixes are separate assemblies, so the analyzer no longer references
  `Microsoft.CodeAnalysis.Workspaces`, which a command line compilation does not provide. They still
  ship as the one package `ViciOne.ServiceBus.Analyzers`.
- Every project builds at the SDK warning level and on C# 14.
- The Apache-2.0 licence text moved from `LICENSE` to `LICENSE.txt` unchanged.

### Migration from MassTransit-style APIs

ViciOne.ServiceBus intentionally exposes one greenfield call form per application operation. There
are no compatibility shims; update call sites directly.

#### Changed call forms

| Old form | New form | Reason |
|---|---|---|
| `endpoint.Send(message, ...)` | `endpoint.SendAsync(message, cancellationToken)` or `SendAsync(message, SendOptions, cancellationToken)` | Makes asynchronous behavior explicit and replaces callback/pipe ambiguity with one typed options record. |
| `provider.GetSendEndpoint(address)` | `provider.GetSendEndpointAsync(address, cancellationToken)` | Uses the standard asynchronous suffix and a final cancellation token. |
| `publishEndpoint.Publish(message, ...)` | `publishEndpoint.PublishAsync(message, cancellationToken)` or `PublishAsync(message, PublishOptions, cancellationToken)` | Provides one typed publish shape and causal cancellation. |
| `requestClient.GetResponse<T>(request, ...)` | `requestClient.GetResponseAsync<T>(request, cancellationToken)` or `GetResponseAsync<T>(request, RequestOptions, cancellationToken)` | Separates deadline and cancellation and removes overload-specific timeout types. |
| `scheduler.ScheduleSend(...)` | `scheduler.ScheduleSendAsync(destination, dueAt, message, cancellationToken)` or the `ScheduleOptions` overload | Uses `DateTimeOffset`, names the due instant, and provides one application shape. |
| `scheduler.SchedulePublish(...)` | `scheduler.SchedulePublishAsync(dueAt, message, cancellationToken)` | Uses the asynchronous convention and an unambiguous due instant. |
| `scheduler.CancelScheduledSend(...)` | `scheduler.CancelScheduledSendAsync(scheduled, cancellationToken)` | Makes I/O and cancellation visible in the name and signature. |
| `consumer.Consume(context)` | `consumer.ConsumeAsync(context)` | Identifies the callback as asynchronous; its context already carries the cancellation token. |
| `filter.Send(context, next)` | `filter.SendAsync(context, next)` | Applies the same callback convention while retaining context-owned cancellation. |
| Pipe, callback, `object`, and anonymous-value send/publish overloads | Typed application overloads; specialized forms under `ViciOne.ServiceBus.Advanced` or `.Advanced.Initializers` | Keeps the application surface small without removing extension capabilities. |
| `context.Send(...)` / `context.Publish(...)` from a consumer | `context.Outgoing.SendAsync(...)` / `context.Outgoing.PublishAsync(...)` | Makes participation in the configured reliable outbox explicit. |
| Direct response buffering helpers | `context.DeferResponse(response)` | Defers the response until successful consumer completion. |
| `UseInMemoryOutbox`, `AddEntityFrameworkOutbox`, or `UseBusOutbox` | `bus.UseReliableMessaging(reliable => ...)` with one selected store | Unifies outbox, inbox, scheduling, retry, and quarantine ownership. |
| Separate durable-sender registration | `bus.UseReliableMessaging(...)` plus `IDurableSender<TBus>.SendAsync(...)` | Uses the same outbox store and delivery service as transactional outgoing messages. |
| Standalone scheduler registration | `UseReliableMessaging(...)` with the stored scheduler, `UseQuartzScheduler()`, or `UseTransportScheduler()` | Requires an explicit scheduling owner and prevents silent fallback. |
| `ConnectMessageJournal(...)` during bus creation | `bus.UseMessageJournal(journal => ...)` | Gives store, sanitization policy, and finite runtime options one validated owner. |
| Separate payload-admission registration | `bus.Limits(MessageLimits.Conservative)` or explicit `MessageLimits` | Makes send and receive size boundaries mandatory for every bus. |
| `ITransactionalBus` | `IAmbientTransactionBus` or `IBufferedBus` | Separates ambient transaction ownership from explicit FIFO buffering. |
| `new Vertex(type, targetType, title, isComposite)` | `StateMachineGraphNode.CreateState(name)`, `CreateEvent(name, messageType, isCompositeEvent)`, or `CreateException(exceptionType)` | Replaces runtime-type sentinels with a valid semantic state/event/exception model. |
| `new Edge(from, to, title)` and `graph.Vertices` | `new StateMachineGraphEdge(source, target, kind)` and `graph.Nodes` | Uses domain-specific names and explicit relationship kinds, removes the inaccessible redundant edge title, and exposes an immutable graph snapshot. |
| Direct use of `GraphStateMachineVisitor<TSaga>` | `stateMachine.GetGraph()` | Keeps traversal state internal while preserving graph inspection as the supported operation. |

#### Moved namespaces and provider names

| Old location | New location | Reason |
|---|---|---|
| Mixed application and infrastructure types in the root namespace | Application contracts only in `ViciOne.ServiceBus` | Keeps the default import focused on sending, consuming, requesting, scheduling, and durable send. |
| Service-registration extensions in product namespaces | `Microsoft.Extensions.DependencyInjection` | Follows the standard .NET discovery location for `services.AddViciOne...` methods. |
| Bus builders and option types mixed with runtime contracts | `ViciOne.ServiceBus.Configuration` | Groups configuration-time APIs separately from runtime messaging contracts. |
| Pipes, filters, definitions, binders, serializers, topology, observers, and initializers in mixed namespaces | `ViciOne.ServiceBus.Advanced` with `.Middleware`, `.Serialization`, `.Topology`, `.Observers`, `.Registration`, and `.Initializers` | Gives extension authors an explicit SPI without crowding the application API. |
| Transport implementation contracts in core namespaces | `ViciOne.ServiceBus.Providers.Transports` | Isolates provider-facing transport SPI. |
| Store and dispatcher contracts in feature namespaces | `ViciOne.ServiceBus.Providers.Persistence` | Isolates persistence SPI and retained-record contracts. |
| Quarantine, snapshots, health, and operator actions in feature namespaces | `ViciOne.ServiceBus.Operations` | Separates operational control from producer messaging. |
| Harness contracts mixed with product runtime types | `ViciOne.ServiceBus.Testing` and provider `.Testing` namespaces | Prevents test APIs and dependencies from entering shipping application packages. |
| `ViciOne.ServiceBus.RabbitMqTransport` / package spelling `RabbitMQ` | `ViciOne.ServiceBus.RabbitMq` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.ActiveMqTransport` / package spelling `ActiveMQ` | `ViciOne.ServiceBus.ActiveMq` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.AmazonSqsTransport` / package spelling `AmazonSQS` | `ViciOne.ServiceBus.AmazonSqs` | Aligns project, package, assembly, and namespace spelling. |
| `ViciOne.ServiceBus.Azure.ServiceBus.Core` | `ViciOne.ServiceBus.AzureServiceBus` | Uses one provider identity across project, package, assembly, and namespace. |
| `ViciOne.ServiceBus.EventHubIntegration` / package spelling `EventHub` | `ViciOne.ServiceBus.EventHubs` | Uses the provider's plural product name consistently. |
| SQL transport provider-specific mixed spellings | `ViciOne.ServiceBus.SqlTransport.PostgreSql` and `.SqlServer` | Makes the database provider explicit and consistent. |
| `ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | `ViciOne.ServiceBus.EntityFrameworkCore` | Matches the package and namespace to the provider capability. |
| `ViciOne.ServiceBus.DynamoDbIntegration` | `ViciOne.ServiceBus.DynamoDb` | Removes the redundant integration suffix. |
| `ViciOne.ServiceBus.QuartzIntegration` | `ViciOne.ServiceBus.Quartz` | Aligns project, package, assembly, and namespace. |
| Analyzer implementation and package under one assembly name | `ViciOne.ServiceBus.Analyzers` and `ViciOne.ServiceBus.Analyzers.CodeFixes`, shipped by `ViciOne.ServiceBus.Analyzers` | Keeps compiler-only analyzer dependencies separate from workspace-based code fixes. |
| `ViciOne.ServiceBus.Visualizer` | `ViciOne.ServiceBus.StateMachineVisualizer` | Aligns the public namespace with the package and assembly identity. |

#### New capability packages

| New package | Contains | Reason |
|---|---|---|
| `ViciOne.ServiceBus.Sagas` | Saga contracts, repositories, state machines, and correlation | Makes saga capability opt-in. |
| `ViciOne.ServiceBus.Courier` | Routing slips and activities | Keeps activity orchestration out of core messaging. |
| `ViciOne.ServiceBus.Futures` | Future orchestration | Declares its Saga and Courier dependencies explicitly. |
| `ViciOne.ServiceBus.JobService` | Job consumers and job coordination | Declares its Saga dependency explicitly. |
| `ViciOne.ServiceBus.Mediator` | In-process mediation | Makes the broker-free mediator independently selectable. |
| `ViciOne.ServiceBus.Initializers` | Anonymous-value and object initialization extensions | Keeps initializer convenience overloads outside the application surface. |
| `ViciOne.ServiceBus.EntityFrameworkCore.Sagas` | EF Core saga, future, and job persistence | Lets reliable messaging and the journal use EF Core without loading saga capabilities. |
