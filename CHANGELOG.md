# Changelog

ViciOne.ServiceBus is an unreleased fork of MassTransit 8.5.10. These records describe the source redesign,
removed capabilities, corrected defects, and changes needed by applications migrating from
MassTransit. The [live repository diff](license/repository_diff.py) compares all tracked source,
tests, and other files between the upstream `MassTransit/v8.5.10` tag and the
latest `main` commit.

## Unreleased — remote checkpoint, 8 October 2026

- Preserved the accepted local-completion source and tests described below, their bounded
  verification receipts, and the current unfinished research as a recoverable source checkpoint.
  This checkpoint does not close aggregate replay, current coverage, CI, cloud or device gates.
- Added exactly one previously accepted typed SQS registration method to the packed public API
  inventory. The other 20,118 inventory lines are unchanged. The original package pipeline's
  baseline comparison failure remains recorded; no retrospective whole-pipeline pass is claimed.
- Classified the two reported historical secret alerts from commits `be715fa5` and `211e73fd`
  as source-context false positives: a connection-string parsing test and a denylist of known
  default credentials. The independent classification is retained; no external alert was dismissed.
- Retained the new package-consumer runtime checks solely as unfinished review evidence.
  A missing namespace import caused the first build failure, which contributes no runtime result.
  The corrected fixture passed Send, Publish and Request/Response, then its retry observation
  predicate waited on a list that retains the first message identity. The four later cases were
  not reached. Fixing that fixture and completing all eight runtime checks remain open.
- Retained an incomplete consumer-lock validator correction candidate. Its four identified
  schema/hash/path issues, unfinished documentation and absent execution/review remain explicit;
  the candidate has not replaced the canonical verification script.

## Unreleased — local completion continuation, 7 October 2026

- Reconciled the execution plan, status and dated coverage page with the completed API audit.
  Corrected obsolete cache/time TODO descriptions while preserving the actual remaining contracts
  and every external obligation. No deferred engineering item is closed by this documentation work.
- Reproduced commit `6b451cff` in a fresh clean checkout: locked Engineering and Unit restores,
  strict Release builds with zero warnings/errors, and all 13,460 native Unit cases across 23
  modules with zero failures/skips. Remaining separate provider profiles, post-correction aggregate
  replay and current coverage remain pending.
- Corrected three Core deadlines to use the selected context TimeProvider: outbox delivery,
  automatic stop after unexpected consume-loop exit, and retained-consumer shutdown grace.
  Outbox delivery retains the operation clock selected before asynchronous loading. Fresh package
  contracts pass all 13 cases, the complete Core regression passes 7,958 cases, and four compiled
  single-cause counter-mutations fail exactly their expected cases with healthy controls. The
  unchanged corrected package passes all 13 cases again after the mutations. Separate source,
  native-result, package/cache and PE/PDB reviews are accepted in the
  [bounded Core deadline receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-core-deadlines/ROOT_CORE_DEADLINE_ACCEPTANCE_R1.json).
  Adapter deadlines and broader time normalization remain separate work.
- Added complete per-command MSBuild binlog provenance to the current coverage producer and
  aggregator. Restore, build and test stages require fresh nonempty logs, retained hashes and
  exact run ownership. Ten Python orchestration contracts and finite original counterexamples
  protect missing, empty, pre-existing, changed and cross-run evidence rejection. Separate
  adversarial tool review is accepted; actual current coverage measurement remains pending.
- Executed the unchanged local-provider baseline: 645 native cases across ten modules passed
  with zero failures/skips using an independently reviewed, lifetime-owned local SSH forwarding
  experiment. Own forwarding processes and run-scoped Compose containers were completely retired.
  Independent runtime evidence acceptance is recorded; the historical SQL missing-delivery cause is
  not closed by this passing rerun.
- The separate unchanged RabbitMQ baseline also passed its complete native profile: 38 cases,
  zero failures/skips, source-bound DLL/PDB and binlog proof, and complete own-resource cleanup.
  Independent runtime acceptance is recorded; this is a local baseline before the Core correction.
- The separate unchanged SQL Server baseline passed its complete native profile: 99 cases,
  zero failures/skips, independently bound runtime/build evidence and complete own-resource cleanup.
  The historical 999/1,000 missing-delivery cause remains a separate investigation.
- Corrected the shared Activity lifetime helpers: ordinary `CurrentChanged` failures cannot escape
  restore/cleanup, stopped parents are replaced by their nearest live ancestor, and exception-event
  preparation cannot replace the original failure. Error status remains available when event timing
  fails. All eight new cases and all 7,966 Core cases pass; three freshly compiled single-cause
  counter-mutations expose five finite expected failures with 19 healthy controls, followed by
  eight unchanged green postchecks. Separate source/test and native artifact reviews are accepted
  in the [bounded Activity stage-A receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_A_ACCEPTANCE_R1.json).
  At stage-A acceptance, other source activation, batch handoff, required monitor and optional
  elapsed observations remained open; subsequent accepted packages are recorded below.
- Isolated typed trace-source construction from independent host metrics. An ordinary source-listener
  failure leaves tracing inactive while actual host counters remain available; the host retains meter
  ownership after instrumentation disposal. Both new cases and all 7,968 Core cases pass. Three
  freshly compiled single-cause mutants produce three finite failures and three healthy controls;
  the unchanged final postcheck passes both cases. Independent source/test and native artifact
  reviews are accepted in the [bounded typed-source receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_B1_ACCEPTANCE_R1.json).
  At stage-B1 acceptance, cold monitor/timeline, optional duration preparation and metric balance remained open.
- Protected the batch Activity handoff so an ordinary trace callback cannot prevent the actual
  consumer pipe or replace its success, business failure or context cancellation. All six new cases
  and all 7,974 Core cases pass. One freshly compiled handoff reversion produces three finite
  expected failures and three healthy controls; all six unchanged final postchecks pass. Separate
  source/test and native artifact reviews are accepted in the [bounded batch handoff receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_B2_ACCEPTANCE_R1.json).
  At stage-B2 acceptance, cold monitor/timeline, optional duration preparation and metric balance remained separate work.
- Isolated optional retry and delivery-lag metric preparation so an ordinary diagnostic getter or
  clock error cannot leak the active-process count. Real handler outcomes and completion durations
  remain intact. Eight new cases and all 7,982 Core cases pass; two compiled guard reversions
  produce six finite expected failures and ten healthy controls, followed by eight unchanged
  green postchecks. Separate source/test and native reviews are accepted in the
  [bounded process-metric receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C1_ACCEPTANCE_R1.json).
  At stage-C1 acceptance, cold monitor/timeline and optional durable elapsed preparation remained open.
- Kept durable consumer completion authoritative when optional timestamp capture or elapsed
  observation fails. Actual generation fencing, UTC, storage errors and cancellation remain
  intact; the completion counter still records the real result and unknown durations emit no
  histogram sample. Nine new cases and all 7,991 Core cases pass; two compiled guard reversions
  produce four finite expected failures and fourteen healthy controls, followed by nine unchanged
  green postchecks. Separate source/test and native reviews are accepted in the
  [bounded consumer-completion receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C2_ACCEPTANCE_R1.json).
  At stage-C2 acceptance, delivery-duration preparation and cold monitor/timeline lifecycle remained open.
- Preserved actual durable delivery outcomes when optional timestamp capture or elapsed
  observation fails. Required UTC, fenced state transitions, storage failures and cancellation
  remain authoritative; unknown durations emit no histogram sample and outcome counters remain.
  All 63 new cases and 8,054 Core cases pass. Two compiled guard reversions produce 38 finite
  expected failures with 88 healthy controls; all 63 unchanged postchecks pass. Separate
  adversarial source/test and native reviews are accepted in the
  [bounded delivery-clock receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C3_ACCEPTANCE_R1.json).
  At stage-C3 acceptance, snapshot cadence/preparation, cold monitor/timeline and EF elapsed preparation remained open.
- Bounded optional durable-snapshot UTC preparation and cadence without changing required
  claim/delivery UTC or actual state transitions. Very large positive intervals and exact maximum
  UTC no longer escape or trigger repeated preparation; successful observations preserve the
  minimum interval after slow delivery. All 13 new cases and 8,067 Core cases pass. Four compiled
  one-cause mutants yield nine finite failures with 43 healthy controls; all 13 unchanged
  postchecks pass. Separate adversarial source/test and native reviews are accepted in the
  [bounded snapshot receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C4_ACCEPTANCE_R1.json).
  At stage-C4 acceptance, cold monitor/timeline lifecycle and EF elapsed preparation remained open.
- Isolated cold optional timeline source failures and preserved required monitor initialization
  and constructor-primary failures. Seven new parent Facts each run one fresh native child;
  all seven corrected cases and 8,074 Core cases pass. Four final compiled one-cause mutants
  yield four finite child failures with 24 healthy controls; all seven unchanged postchecks pass.
  The original nonthrowing source case is healthy. A redundant restoration mutant survived,
  was not counted as a kill, and its helper was removed before the complete final rerun.
  Separate adversarial source/test and native reviews are accepted in the
  [bounded cold-source receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C5_ACCEPTANCE_R1.json).
  At stage-C5 acceptance, required public caller cleanup and EF elapsed preparation remained open.
- Preserved action, setup and wait failures across required public caller cleanup, while
  successful calls still report required cleanup errors and retain nested owner aggregates.
  Observations release before the tracked root stops. All 38 corrected cases and 8,112 Core
  cases pass; six compiled single-cause mutants yield 34 finite failures and 194 healthy
  controls, followed by all 38 unchanged green postchecks. The original has 17 finite failures
  and 21 healthy controls. Discovery and compiler failures are excluded from product proof.
  Independent source/test and native reviews are bound in the
  [public caller cleanup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C6_ACCEPTANCE_R1.json).
  At stage-C6 acceptance, EF optional duration preparation remained next.
- Kept classic EF receive-outbox transactions and database failures authoritative when optional
  selected-clock measurement fails. Required fault notification remains forwarded and awaited;
  its fallback is the actual System-clock duration of the current attempt. All 13 new SQLite
  cases and all 432 local EF cases pass. Five compiled single-cause mutants yield 19 finite
  failures and 46 healthy controls; all 13 unchanged postchecks pass. The original has five
  finite failures and eight healthy controls. A forced rebuild binds the correction; stale
  incremental-build attempts receive no corrected-source credit. Separate source/test and native
  reviews are accepted in the [bounded classic EF duration receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C7_ACCEPTANCE_R1.json).
  Current coverage and broader Unit/provider/schema/CI gates remain open.
- Made cache index publication and retirement robust against ordinary throwing comparers and
  later required-clock/lookup faults. Preparation finishes before live publication, and exact
  owned slots unlink committed/pending entries without invoking user comparison. Failed bulk
  indices leave existing resources intact; late old factories cannot unlink a replacement.
  All 30 focused cases, 142 complete Cache cases and 8,142 Core cases pass. Eight compiled
  single-cause mutants expose 12 finite failures with 228 healthy controls; the unchanged
  final postcheck passes the same 30 cases. Separate child events and source/runtime evidence
  are bound in the [bounded cache repair receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_COMPARER_RETIREMENT_ACCEPTANCE_R1.json).
  Cache observer ordering/failure-channel/backpressure contracts and measured resource budgets
  remain separate work, alongside adapters/startup, aggregate replay, current coverage and CI.
- Continued locally feasible work under the [local completion plan](.testagent/local-completion-plan.md).
  Real cloud resources and target-device hardware are unavailable; GitHub workflow activation
  remains gated by local acceptance and separate CI qualification.

- Corrected typed Amazon SQS startup validation to select the named bus options. Malformed
  Region, Scope and Scope/Region combinations fail through the actual Generic Host before the
  chosen pre-runtime boundary; an unused malformed default no longer blocks a coherent typed bus.
  All six package cases and 417 local SQS cases pass. Six compiled single-cause counter-mutations
  expose 15 finite first failures with 21 healthy controls; the restored correction passes all six
  postchecks with zero warnings/errors. The scoped SDK compatibility check retains the original
  public contract and identifies exactly one added generic `UsingAmazonSqs<TBus>` method.
  Independent source, lock, native, mutation and final reviews are accepted in the
  [bounded SQS startup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_SQS_STARTUP_STAGE_A_ACCEPTANCE_R1.json).
  Other startup owners and the global packed API baseline remain open; these cases stop at a
  sentinel before actual ServiceBus/AWS runtime startup.

- Passed all 30 current local Azure Service Bus emulator cases with zero failures/skips,
  source-bound runtime evidence and complete own-resource teardown. The separately reviewed
  pre-health forwarding tool has 18 passing contracts, five finite counterproof failures with
  four healthy controls, and 18 unchanged postchecks. Its tool slots are not native product cases.
  See the [bounded current ASB receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package03-infrastructure/asb-current-native-r3/ROOT_CURRENT_ASB30_FUNCTIONAL_ACCEPTANCE_R1.json).
- Prevented known run credentials from reaching persisted broker logs or collection warnings.
  All nine focused contracts and all nine canonical postchecks pass after independent review.
  The original affected broker log remains private, with a separately hashed redacted copy.
  See the [bounded log repair receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package03-local-providers/broker-log-redaction-owner-r1/ROOT_BROKER_LOG_REDACTION_ACCEPTANCE_R1.json).
  Three negative broker entity-size diagnostics remain a new open local investigation; the
  passing native cases do not resolve their cause. Cloud and hardware acceptance remain open.

- Added four actual Generic Host startup cases for selected named RabbitMQ Host/TLS validation,
  isolation of unused malformed default/other names, and a second invalid selected bus. All four
  and the complete RabbitMQ Unit module (599 unique cases) pass without failures/skips. Five
  compiled single-cause variants expose 19 finite first failures and 41 healthy controls over
  60 repeated slots; the restored original passes all twelve selected startup cases after a
  strict Release rebuild. Independent source/native/PE/PDB reviews accepted the exact four test,
  project, generated-lock and requirement files now applied. Existing product registration
  satisfies these contracts; the coherent case stops at an owned pre-runtime sentinel. Other
  startup owners, broker connectivity, full runtime startup and global release remain open.
  See the [bounded RabbitMQ startup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_RABBIT_SELECTED_NAMED_HOST_ACCEPTANCE_R1.json).

- Added three selected named Azure Generic Host cases. All 607 Unit slots pass, with 604
  shortened display labels and two documented theory collision groups. Five compiled variants
  expose 14 finite first failures and 16 healthy controls across 30 repeated slots; the restored
  six cases pass. Existing registration was correct; the coherent case ends at a pre-runtime
  sentinel. See the [bounded Azure startup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_AZURE_SELECTED_NAMED_HOST_ACCEPTANCE_R1.json).
- Fixed `CORE-HOST-STARTUP-CLEANUP-01`: rejected startup options no longer also break cleanup
  of a never-started runtime. Invalid startup remains rejected; pre-canceled Stop remains
  retryable with the exact caller token. Four new fixture cases plus a lifecycle control pass,
  including actual InMemory message delivery; all 8,146 Core cases pass. Four compiled variants
  expose six finite first failures and 14 healthy controls across 20 repeated slots; all five
  restored postchecks pass after a strict rebuild. Independent source/native/PE/PDB reviews
  accepted the exact three code/test/requirement files. Partial-start/race, other startup,
  coverage, cloud and hardware work remain open. See the [bounded Core cleanup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_CORE_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json).

- Added three actual Generic Host composition contracts: both missing transport/limits diagnostics,
  eager selected typed payload validation, and separate default/typed InMemory delivery with exact
  start/stop and unused optional payload isolation. All three and all 8,149 Core cases pass; four
  compiled single-cause variants produce four finite first failures and eight healthy controls over
  twelve repeated slots. Fresh strict rebuild restores all seventeen runtime files exactly and
  all three cases pass again. Independent source, binary/native and actual two-file application
  reviews are accepted. Existing product registration was correct; only tests and requirement
  mappings changed. Other startup owners, provider runtime, current coverage and aggregate gates
  remain open. See the [bounded composition receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_COMPOSITION_GENERIC_HOST_ACCEPTANCE_R1.json).

- Added 57 cache observer contracts covering committed state, complete fan-out, awaited callbacks
  and best-effort diagnostics across add/removal/clear, including missing/disabled/throwing logging.
  Repaired an existing asynchronous Clear test's teardown so early assertions cannot strand disposal.
  All 59 selected and 8,206 Core cases pass (199 Cache cases); three compiled single-cause variants
  produce 117 finite first failures and 60 healthy controls over 177 repeated slots of the same
  59 identities. After restoration, the same 59 pass and all seventeen runtime files match the
  original bytes. Independent source, compiled/native and actual application reviews accept three
  test/requirement files; no product code changed. Diagnostics remain best effort; ordering,
  backpressure, caller memory and measured cache performance remain open.
  See the [bounded observer receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_OBSERVER_FAILURE_CHANNEL_ACCEPTANCE_R1.json).

- Corrected Reliable Generic Host cleanup after invalid selected options: defer policy resolution
  until runtime execution while preserving startup validation. Three new actualHost cases and ten
  existing options/composition cases pass; all 8,209 Core cases pass. The existing unit theory now
  validates the registered options phase explicitly and preserves exact typed diagnostics without
  catching its own assertion failure. Four product variants and one test-boundary control expose
  15 finite first failures and 50 healthy controls over 65 repeated slots; all thirteen restored
  postchecks pass after a strict rebuild and exact restoration of seventeen runtime files.
  Independent source, native, compiled and actual four-file application reviews are accepted in the
  [bounded Reliable startup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_RELIABLE_GENERIC_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json).
  Other startup owners and durable persisted-delivery/recovery contracts remain separate work.
- Corrected EF Generic Host cleanup after invalid selected EF/inbox/scoped-delivery options:
  defer constructor-time settings/source resolution until registered validation or explicit Start.
  Four new Host cases and the corrected existing direct-worker boundary case pass; all 436 local
  EF slots (435 short labels) and a separate 8,209-case affected Core regression pass. Three
  compiled single-cause controls yield six first finite failures and nine healthy controls across
  fifteen repeated slots. The restored post-run passes all five; runtime19 bytes match Fixed5.
  Exactly seven product/test/project/lock files are independently reviewed and applied. Persisted
  delivery and broader engineering gates stay open. See the
  [bounded EF startup cleanup receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_EF_GENERIC_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json).

- Added four bounded cache retirement/dispatch contracts: charged retirement at two held phases,
  dispatch-time observer snapshots, awaited batch serialization and active inherited reentrancy.
  All four and all 8,213 Core cases pass (203 Cache); four single-cause variants expose five finite
  failures and eleven healthy controls over sixteen repeats of those same four identities. The
  restored four pass again with all seventeen runtime files byte-identical to the original. Two
  test/requirement files are applied and independently rechecked; existing product behavior was
  correct. Broader ordering, fairness and measured resource budgets remain open. See the
  [bounded receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_RETIREMENT_DISPATCH_ACCEPTANCE_R1.json).

- The bounded Azure receiver cancellation-clock package is accepted on 8 October: normal and
  session receivers use the selected context TimeProvider for shutdown grace. Two original finite
  clock failures are corrected; all six focused cases and all 613 native Azure cases pass (610
  shortened labels, with two inherited theory collision groups). Four compiled one-cause controls
  produce four finite failures and twenty healthy results over 24 repetitions of the same six cases;
  the restored six pass again and all 18 runtime files are byte-identical to the corrected run.
  Null-grace behavior and broker-registration retirement by callback completion are protected.
  Joining the broker registration before timeout cleanup is defensive source reasoning, not a
  reproduced concurrent race. The exact six-file transfer has independent after-application review.
  The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-adapter-deadlines/ROOT_AZURE_RECEIVER_CLOCK_ACCEPTANCE_R1.json`.
  SQS/SNS batching, aggregate replay and real cloud validation remain open.

### SQS topic lifecycle — 8 October 2026

The bounded SNS topic-lifecycle repair is accepted. Disposed topics reject new work without
creating another batcher; every disposer joins admitted provider work. The client checks caller
cancellation before lookup and retries at most once only after stale-resource or proven pre-write
admission rejection. Already admitted SDK failures retain their identity and are never replayed.
The unchanged eleven focused cases pass, as do all 428 local SQS module cases with zero skips.
Seven compiled one-cause controls expose fourteen first finite failures and sixty-three healthy
results over 77 repetitions of the same eleven identities; independent fresh checks bind 70 PE
parse slots and 212 compiled METHOD mappings per variant. The forced corrected post-run passes
all eleven again and restores all fifteen runtime files byte-for-byte. Exactly four product/test/
requirement files are independently reviewed and applied, preserving public signatures and all
203 old requirement rows. SQS/SNS batching clocks, live cloud and broader races remain open.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-adapter-deadlines/ROOT_SQS_TOPIC_LIFECYCLE_ACCEPTANCE_R1.json`.

### Event Hubs checkpoint clock — 8 October 2026

Event Hubs checkpoint batches now use the selected endpoint TimeProvider through receiver,
processor, partition and batch timer. Existing public constructors keep their System fallback.
The two new Facts prove real SDK checkpoint admission, the seven-second selected timer,
partial-batch expiry through virtual time, and the legacy full-batch constructor boundary.
Both pass; the affected six-case SPI/clock replay and the whole local Event Hubs module also
pass: 196 native slots, zero failures/skips (194 visible labels, one triplicate URI label).
Five compiled single-cause controls each expose one first finite failure while the legacy case
remains healthy: ten repetitions of the same two Facts, not ten new tests or five defects.
Every variant has independently checked fresh eleven PE/PDB images and all 100 compiled
METHOD mappings. The forced corrected post-run passes both Facts and restores all sixteen
runtime files byte-for-byte to the accepted six-case build. Exactly ten product/test/project/
lock/requirement files are independently reviewed and applied; old public signatures, all 98
ordered requirement rows and all 65 previous lock objects remain unchanged.
The original whole-module run with two strict-proxy setup failures is retained; those existing
proxies now allow the normal optional TimeProvider lookup without relaxing any assertion.
SQS/SNS batching clocks, aggregate replay, live cloud and broader races remain open.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-adapter-deadlines/ROOT_EVENTHUB_SELECTED_CLOCK_ACCEPTANCE_R1.json`.

### SQS/SNS batch clock — 8 October 2026

SQS send/delete and SNS publish batches now use the connection-owned TimeProvider captured
before the first entity-resolution await. Later payload or operation-scope updates do not replace
that provider; cache expiration keeps its separate clock. Existing public constructors retain their
System fallback. The selected timer-failure case closes admission and faults both queued entries
with the original cause before cleanup, even when optional error logging throws.
The same nine original cases expose eight finite failures and one healthy legacy control;
the corrected nine and all 437 unique local SQS module cases pass with zero failures/skips.
Six compiled single-cause controls expose nineteen first finite failures and seven healthy results
over 26 repetitions of those nine cases. The forced corrected post-run passes all nine again and
restores all fifteen runtime files byte-for-byte to the accepted fixed/whole image.
Fresh checks cover seventy PE/PDB parse slots and 20,993 document checksums over six controls
and the post-run, with all 216 compiled METHOD mappings checked per image. Exactly eleven
product/test/requirement files are independently reviewed and applied. Public metadata signatures,
defaults, constraints and nullability remain unchanged; all 212 ordered old requirement rows remain.
These proofs cover the stated batch-clock and selected-failure boundaries, not general queue
fairness, all concurrent cleanup paths, real AWS behavior or the complete time inventory.
The SQS visibility-renewal caller is a separate source-inventory follow-up awaiting matched native
proof; current aggregate/provider replay, packed API, coverage, CI, cloud and device gates stay open.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-adapter-deadlines/ROOT_SQS_BATCH_SELECTED_CLOCK_ACCEPTANCE_R1.json`.

## Unreleased — API review completion, 6 October 2026

The source API review and repairs are complete within their recorded contract and
local validation boundaries: all 234 confirmed audit findings have a bounded
acceptance, with no confirmed repair pending. The current inventory accounts for
18,600 exposed source symbols; the contract register also retains 833 accessor
representations, which are counted separately.

The final local checks passed 13,460 unit tests across 23 modules and 166 selected
Event Hubs tests, and verified all 31 delivery packages. Real provider and cloud
workflows, the excluded emulator tests, and ARM64 operation with 512 MiB without
swap remain separate validation work. This is an unreleased source checkpoint,
not a production or universal A+ certification.

See the [completed review corrections and verification boundaries](docs/changelog/product-fixes.md#api-review-completion-6-october-2026)
and the [final bounded acceptance](evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE551_FINAL234_CURRENT18600_LOCAL_GATES_ROOT_ACCEPTANCE_R1.json).
Earlier entries and coverage pages retain their dated checkpoints.

## By topic

- [Source changes by area](docs/changelog/source-areas.md): identity, API, core messaging,
  reliability, transports, workflows, diagnostics, build and test structure.
- [Product defects corrected during source review](docs/changelog/product-fixes.md):
  the detailed chronological defect record since 6 September 2026.
- [Verification and regression tests](docs/changelog/verification-history.md):
  behavior, failure, boundary, and concurrency checks added during review.
- [Removed capabilities](docs/changelog/removed-capabilities.md) and
  [other changes](docs/changelog/other-changes.md).
- [Migration from MassTransit-style APIs](docs/changelog/migration-from-masstransit.md):
  changed call forms, moved namespaces, and new capability packages.
- [Dated coverage and review checkpoints](docs/quality-status.md): measured coverage,
  CRAP and historical open-finding records; the API audit completion is recorded above.

The detailed documents preserve the original chronological entries. The source-area overview
helps readers locate the changes relevant to each module. Git contains the exact patch and the
archived review artifacts.
