# ServiceBus API repair plan

## Current execution

The API audit is complete within its recorded source and local-validation boundaries: 234 confirmed
findings accepted, zero pending confirmed repairs. The completed authority is
`evidence/SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json`; its remote backup and fast-forward to `main` bind
commit `6b451cff07f46c6bf3eee325ff81ead1089e8c89`. This is not a global A+ or release verdict.

The Product Owner authorized autonomous completion of the remaining local work on 7 October 2026.
Follow [local-completion-plan.md](local-completion-plan.md) for its execution order and `../TODO.md`
for deferred engineering obligations. Real cloud and target hardware remain unavailable.

The continuation has accepted fresh baseline reproducibility, the bounded three Core deadlines,
coverage-binlog tool provenance, unchanged local-provider baselines (645 / ten modules, RabbitMQ38,
SQLServer99), and shared Activity stage A (Core7,966 / eight new cases / three compiled mutants /
eight green postchecks), and typed source stage B1 (Core7,968 / two new cases / three compiled
mutants with three finite failures and three healthy controls / two green postchecks).
Batch handoff stage B2 is also accepted (Core7,974 / six new cases / one compiled mutant with
three finite failures and three healthy controls / six green postchecks).
Process-metric balance stage C1 is accepted: eight new cases / 7,982 Core cases / two compiled
guard mutants with six finite failures and ten healthy controls / eight unchanged green postchecks.
Durable consumer completion stage C2 is accepted: nine new cases / 7,991 Core cases / two
compiled guard mutants with four finite failures and fourteen healthy controls / nine unchanged
green postchecks. Actual process-local generation-fenced store results and required UTC/errors/
cancellation remain intact; unknown duration is omitted while the completion counter remains.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C2_ACCEPTANCE_R1.json`.

Durable delivery diagnostic-clock stage C3 is accepted: 63 new cases / 8,054 Core cases / two
compiled guard mutants with 38 finite failures and 88 healthy controls / 63 unchanged green
postchecks. Actual process-local state transitions, required UTC/store errors and cancellation
remain authoritative; unknown duration emits no histogram sample and outcome counters remain.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C3_ACCEPTANCE_R1.json`.
At stage-C3 acceptance, snapshot cadence/preparation, cold monitor/timeline lifecycle and EF elapsed preparation remained open.

Durable snapshot preparation/cadence stage C4 is accepted: 13 new cases / 8,067 Core cases /
four compiled one-cause mutants with nine finite failures and 43 healthy controls / 13 unchanged
green postchecks. Required claim/delivery UTC and actual process-local state remain authoritative;
optional UTC faults, very large positive intervals and exact maximum UTC are bounded. Successful
observations retain the minimum configured interval after slow delivery. This is a sequential
hosted-cadence proof; the injected observation cancellation is not a real shutdown proof.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C4_ACCEPTANCE_R1.json`.
At stage-C4 acceptance, cold monitor/timeline lifecycle and EF elapsed preparation remained open.

Cold telemetry source and constructor cleanup stage C5 is accepted: seven new parent Facts,
each selecting one fresh native child case / 8,074 Core cases / four compiled single-cause
mutants with four finite child failures and 24 named healthy controls / seven unchanged green
postchecks. Optional named timeline initialization remains inert after its cached source failure;
required monitor initialization preserves its original exception. Constructor cleanup cannot
replace the primary failure; normal required timer-disposal failures still propagate.
The nonthrowing source-observer case passes the original. A redundant restoration mutant survived
and received zero kill credit; its unnecessary helper was removed and all final proofs rerun.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C5_ACCEPTANCE_R1.json`.
At stage-C5 acceptance, required public caller cleanup and EF elapsed preparation remained open.
Those bounded results do not qualify all telemetry, current coverage, the full Unit/provider replay,
CI, real cloud or hardware.

Public caller cleanup stage C6 is accepted: 38 new native cases / 8,112 Core cases /
six compiled single-cause mutants with 34 finite failures and 194 named healthy controls /
38 unchanged green postchecks. Action, setup and wait failures remain authoritative while
owned cleanup is attempted; successful bodies still propagate required cleanup errors,
including nested owner aggregates. Observation handles are released before the tracked root
stops. The original has 17 finite failures and 21 healthy controls. One zero-discovery attempt
and one mutant compiler failure receive no product-counterexample credit.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C6_ACCEPTANCE_R1.json`.
At stage-C6 acceptance, EF optional duration preparation remained next.

Classic EF receive-outbox duration stage C7 is accepted: 13 new SQLite cases / 432 complete
local EF cases / five compiled single-cause mutants with 19 finite failures and 46 healthy
controls / 13 unchanged green postchecks. Optional selected-clock measurement cannot prevent
the transaction or replace its database failure. Required fault notification is still forwarded
and awaited; if selected duration measurement fails, it receives the actual System-clock duration
of this attempt. Required UTC and delivery cancellation remain authoritative. The original has
five finite failures and eight healthy controls. Stale incremental-build attempts receive no
corrected-source credit; the accepted correction uses a forced rebuild. Whole EF display labels
include one existing truncated-argument collision; all 432 native events are retained.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C7_ACCEPTANCE_R1.json`.
At stage-C7 acceptance, current coverage and broader Unit/provider/cache/startup/adapter/CI
gates remained open.

Cache comparer/publication and retired-resource ownership repairs are accepted: all 30 focused
cases, 142 complete Cache cases and 8,142 Core cases pass with zero failures/skips. Eight compiled
single-cause mutants produce 12 finite parent failures and 228 healthy controls; their 16 separate
child events are three failures and 13 healthy controls, with no duplicate wrapper credit.
After all mutations, the unchanged corrected source passes the same 30 cases and two fresh children.
The accepted fixed and mutant builds are warning/error-free. Comparer preparation precedes live index publication;
exact slot ownership removes indices and pending state without fallible comparison. Rejected
factory values and resources retired before later required-clock/lookup faults are released.
Failed bulk-index preparation leaves existing ownership intact, colliding head/middle/tail removal
preserves survivors, and late old factories cannot unlink their pending replacement.
The original has 12 finite failures and 18 healthy controls. Its Pending249 binary is retained as
historical evidence; final Pending250 captures a terminal known error before the physical oracle.
Stranded negative-control product requests receive no join credit. Source, runtime, PE/PDB,
Requirements, independent adversarial reviews and unchanged postchecks are bound in
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_COMPARER_RETIREMENT_ACCEPTANCE_R1.json`.
Observer failure-channel/order/backpressure contracts, adapter deadlines, startup validation,
current coverage/CRAP, full Unit/provider replay, developer journeys/benchmarks and CI remain open.
The inherited expiry snapshot allocation remains a local-benchmark item; no heap/RSS or device
resource-envelope result follows from these tests.


Remaining cache observer contracts and adapter/startup packages proceed next; current coverage and broader aggregate gates remain pending.

Named SQS startup stage A is accepted: six actual Generic Host cases / 417 complete local
SQS cases / six compiled single-cause mutants with 15 finite first failures and 21 healthy
controls / six unchanged green postchecks. Selected typed options now validate under the bus
name before the chosen pre-runtime boundary; an unused malformed default cannot reject a
coherent typed bus. Invalid Region, Scope and Scope/Region combinations retain useful owner
diagnostics; selected default validation remains intact. Original six cases had four finite
failures and two healthy controls. All accepted fixed/mutant builds are warning/error free.
The scoped SDK API comparison accepts original-to-fixed compatibility; its reverse control
reports exactly the added generic UsingAmazonSqs<TBus> method. The global packed API baseline
remains open. Positive cases reach an exact sentinel before runtime, with zero client-factory
calls; they do not establish successful full ServiceBus or AWS runtime startup.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_SQS_STARTUP_STAGE_A_ACCEPTANCE_R1.json`.
Other startup owners, current coverage, full Unit/provider replay, adapter deadlines, observer
contracts, broader telemetry schema, CI, real cloud and device qualification remain open.

The current local Azure Service Bus emulator profile is accepted for its 30 unique native
functional cases (zero failures/skips), with locked restore, a forced warning/error-free rebuild,
source-bound runtime evidence and removal of its own processes and Compose resources. This is
current source evidence, separate from the earlier 645/RabbitMQ38/SQLServer99 baseline. Acceptance:
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package03-infrastructure/asb-current-native-r3/ROOT_CURRENT_ASB30_FUNCTIONAL_ACCEPTANCE_R1.json`.

A temporary SQL password echoed in the broker log was repaired at the shared log producer.
Known run credentials are masked before persistence and failed-collection console output;
nine focused contracts and nine canonical postchecks pass, with independent adversarial review.
The original log remains private and byte-identical, with a separately hashed redacted derivative.
The native 30-case run used the original collector; its history is not rewritten as a new run.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package03-local-providers/broker-log-redaction-owner-r1/ROOT_BROKER_LOG_REDACTION_ACCEPTANCE_R1.json`.

A new local diagnostic finding, `ASB-BROKER-INVARIANT-01`, remains open: three broker records
report negative entity sizes during passed dead-letter cases. Their cause and operational impact
are unqualified. Other deletion/session/link diagnostics are only partly correlated; Server GC
is disabled in this emulator and gives no performance acceptance. Passing tests do not make
these diagnostics harmless. The historical 234 audit repairs remain frozen; this new finding
belongs to the continuation. Current coverage, other provider replays, startup/adapter/observer/
telemetry/CI work, real cloud and hardware acceptance remain open.

The bounded named RabbitMQ Generic Host startup package is accepted. Four new actual-host
cases and the complete RabbitMQ Unit module (599 unique native cases) pass without failures
or skips. Five compiled single-cause variants produce 19 finite first assertion failures and
41 healthy controls over 60 repeated slots; the restored original passes all twelve selected
startup cases after a strict warning/error-free rebuild. The four new cases are included in
599 and twelve; these counts are not added as distinct tests.

The cases prove selected Host/TLS validation, isolation of unused malformed default/other names,
and identification of an invalid second selected bus before both callbacks. The coherent case
reaches the exact owned pre-runtime sentinel. Existing product registration was already correct;
this closes these registration-path evidence gaps. Independent source, generated-lock, native and
PE/PDB reviews accepted the exact four test/project/lock/requirements files now in the repository.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_RABBIT_SELECTED_NAMED_HOST_ACCEPTANCE_R1.json`.

The selected named Azure Generic Host package is accepted: three new cases and all 607 native
Unit slots pass (604 shortened display labels, with two documented theory collision groups).
Five compiled single-cause variants expose 14 finite first failures and 16 healthy controls
across 30 repeated slots; the restored six selected cases pass. The coherent case reaches an
owned pre-runtime sentinel; this does not prove Azure broker connectivity. The exact four test,
project, generated-lock and requirement files are applied; existing registration was correct.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_AZURE_SELECTED_NAMED_HOST_ACCEPTANCE_R1.json`.

The new Core finding `CORE-HOST-STARTUP-CLEANUP-01` is closed for a never-started runtime.
Two invalid startup-policy cases reproduced failed Stop/Dispose cleanup; the minimal seven-line
correction avoids re-reading rejected options only before any depot/start task exists. Four
new fixture cases plus an existing lifecycle control pass; all 8,146 Core cases pass. Four
compiled variants expose six finite first failures and 14 healthy controls across 20 repeated
slots, followed by five unchanged green postchecks. The positive case sends and consumes a
nonce through the actual InMemory runtime; a canceled pre-start Stop retains the caller token
and allows later startup. Exact three code/test/requirement files are applied after independent
review. This does not close arbitrary partial-start or concurrent lifecycle races.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_CORE_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json`.

The bounded Core composition Generic Host package is accepted. Three new cases and all 8,149
Core cases pass without failures or skips. Missing transport and limits produce both ordered
startup diagnostics; a selected typed payload limit fails before runtime materialization. Actual
default and typed InMemory buses independently deliver their messages and start/stop exactly once;
an unused typed payload poison is not evaluated. Four compiled single-cause variants produce
four finite first failures and eight healthy controls across twelve repeated slots. A fresh strict
rebuild restores all seventeen runtime files byte-for-byte; the same three cases pass again.
Independent source, compiled requirement/PE/PDB, native and actual application reviews accept the
exact two test/requirement files. Existing product registration was correct; no product code changed.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_COMPOSITION_GENERIC_HOST_ACCEPTANCE_R1.json`.

The bounded cache observer failure-channel package is accepted: 54 hostile combinations and
three healthy cases cover add/removal/clear, committed state, complete fan-out, awaited callback
completion and best-effort Warning diagnostics. Missing, disabled or broken logging preserves
caller success and fan-out; it does not guarantee a durable failure channel. All 59 selected cases
and all 8,206 Core cases pass (199 Cache cases). Three compiled single-cause variants expose
117 first finite failures and 60 healthy controls across 177 repeated slots, representing the same
59 identities. The restored original passes all 59 again after a strict rebuild; all seventeen runtime
files are byte-for-byte original. Independent final review accepts the exact three test/requirement
files now applied, including a teardown correction that releases and joins the actual Clear task
before an early assertion can strand disposal. Product behavior was already correct; no product
code changed. Snapshot/concurrent ordering, reentrancy, retirement backpressure, caller/waiter
memory budgets and measured cache performance remain open.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_OBSERVER_FAILURE_CHANNEL_ACCEPTANCE_R1.json`.

The bounded Reliable Generic Host startup cleanup repair is accepted. Selected default NaN
jitter and typed zero poll options are rejected before endpoint start; subsequent Stop, DI and
observer cleanup remain clean. Policy resolution is deferred from public DI construction to
actual worker execution, with startup validation and direct internal policy construction retained.
Three new actualHost facts plus ten existing options/composition scenarios pass, and all 8,209
Core cases pass. The first whole run exposed five existing test-boundary failures: the unit theory
now explicitly invokes registered options startup validation and checks phase/type/closed owner/
name/full typed diagnostics outside its product-action assertion capture. That red history remains.
Four product variants plus one test-boundary control expose fifteen first finite failures and fifty
healthy controls over sixty-five repetitions of the same thirteen identities. A fresh restored
strict rebuild passes all thirteen again; all seventeen runtime files are byte-for-byte fixed.
Independent source, native, compiled metadata and actual application reviews accept exactly four
product/test/requirement files. This closes RELIABLE-HOST-STARTUP-CLEANUP-01 and the bounded
existing-test issue; it does not prove persisted durable delivery, all startup owners or global A+.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_RELIABLE_GENERIC_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json`.

The bounded EF Generic Host startup cleanup repair is accepted. Selected EF lock-provider,
inbox-delay and scoped-delivery retry errors formerly also broke Host.Stop cleanup. Inbox settings
now resolve at explicit Start/execution/direct cleanup; Core EF sources resolve after registered
startup validation. The existing direct-worker test checks constructor dependencies and explicit
Start validation, with actual worker cleanup joined before assertions. Four new unchanged Host
cases plus that existing boundary case pass; all 436 EF slots pass (435 short labels, one collision),
and a separate fresh affected Core regression passes 8,209 cases. Three compiled single-cause
variants produce six first finite failures and nine healthy controls over fifteen repeated slots;
the corrected post-run passes all five again, restoring all19 firstparty runtime files byteexact.
Independent source, native, physical/compiled binding and after-application reviews accept exactly
seven product/test/project/lock files; other6,098 source files and the frozen234 audit stay unchanged.
This closes EF-HOST-STARTUP-CLEANUP-01 only; persisted delivery, sustained SQL polling and other
startup/provider/coverage/cache/adapter/schema/API/CI/cloud/hardware contracts remain separate work.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_EF_GENERIC_HOST_STARTUP_CLEANUP_ACCEPTANCE_R1.json`.

The bounded cache retirement and dispatch package is accepted. Four new unchanged Facts prove
admission remains charged at separately held removal-observer and async-disposal phases, snapshot
selection after dispatch admission with retention of active observers, serialization of the entire
awaited observer batch, and rejection of inherited active same-cache mutation while another cache
can mutate. All four and all 8,213 Core cases pass, including 203 Cache cases. Four compiled
single-cause variants produce five first finite failures and eleven healthy controls over sixteen
repetitions of the same four identities; the strict restored postcheck passes all four again.
All seventeen firstparty runtime files are byte-for-byte original. Independent source, native,
compiled binding and after-application reviews accept exactly two test/requirement files; product
code was already correct and did not change. These are finite phase observations, not a continuous
trace, FIFO commit-order guarantee, fairness or measured heap/RSS budget. Broader cache resource
contracts and other local/external packages remain open.
Acceptance: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_RETIREMENT_DISPATCH_ACCEPTANCE_R1.json`.

Other startup owners and models, further composition cases and provider full runtime startup remain open. Current coverage, aggregate Unit/provider replay, adapters, cache observer contracts,
telemetry schema, developer journeys, benchmarks, CI, cloud and target hardware are separate work.
The new ASB broker-size invariant finding stays open. The frozen 234 accepted audit repairs and
all 62 external obligation IDs remain unchanged; no global A+ or release acceptance is claimed.

The bounded Azure receiver cancellation-clock package is accepted on 8 October: normal and
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

## Historical API repair execution notes

The entries below preserve the plans as written during the repair work. Their pending/open wording
describes those earlier checkpoints; current repair disposition belongs to the completed ledger.

| Package | Findings | Regression obligations |
|---|---|---|
| 01 Azure client and credential boundaries | 007,008,011,015 | Both SDK delays; delay boundaries; external/internal/mixed owners and terminal shutdown; live session deadline; serialized secret-free probe and unchanged SDK URI |
| 02 Persistent identity and message data | 006,009,023 | Reserved S3 prefix; strict UTF16/UTF8 roundtrip; retained valid Unicode and persistent contract behavior |
| 03 EventHubs and request cancellation | 012,013,014,016,017 | Linked CTS active through completion; complete close under faults; correct timer boundaries; private settings snapshot; caller-owned unresolved waits |
| 04 Persistence | 018,021 | Actual DbContext transaction ownership; stable inbox namespace; independent bus owners; explicit retained-state migration disposition |
| 05 Core diagnostics and time | 004,019,022,027,028,029,030 | DI meter/logger lifetime; failed activity admission cleanup; readonly results; timer range; backward UTC and valid batch controls |
| 06 Endpoint and resource ownership | 031,032,033,034,035 | All endpoint contributions; lease-owned fallback filters; original operation and cleanup faults; activity-specific endpoint lookup; private definition owner preserving Bind equality |
| 07 Benchmarks | 024,025,026 | Valid mediator limits and JSON headers; unique consumed-ID observations; actual BDN Dry |
| 08 Contracts and gates | 001,002,003,005,010,020 | Actual completion XML; clean Unit prerequisite; reviewed final package API baseline; Empty Address and untrusted lookup XML; retained MultiBus DbContext guard |

Per package: inspect complete source/test closure → author regression → confirm original failing oracle → apply production fix → focused native validation → one-cause isolated countermutants → freeze exact code/test patch → separate adversarial review → address findings and refreeze if necessary.
Build/provider operations are sequential. Read-only research may proceed in parallel.
Final: strict shared profiles and package consumers, complete API-review ledger closure, integrated adversarial review, PO report.
No optional upgrade, unrelated modernization, weakened oracle, skip, polling-as-success or baseline update during diagnosis.

Package09 newly researched Azure paths: first compile and execute all six candidate oracles against unchanged causal product sources; promote only confirmed failures. Correct default formatter propagation and retained subscription validation if causal proof holds. Resource naming compatibility for nested queue destinations requires its own source/history/consumer assessment before choosing a naming fix. Each new code/test/projection delta requires exact freeze,mutants and adversarial review.

Package03 F012: install canonical ServiceBusSharedAdministrationLeaseTests (four theories /18cases); build strictly and execute originalclass (expected8red10green). Then four await-within-using production fixes, green18; four direct-return mutants plus linked-caller/lease and disposal/fault controls; actual rollback. Add four existing RequirementCoverage projections after Package09 shared projection review completes. Unfiltered Azure owner remains F038-red pending PO. Freeze all deltas and separate adversarial reviewer who did not author F012 tests.

Package08 docs subset: five fresh consumer modes. Empty checks exact empty exception, HasValue guard, deferred populated null, actual inline-only value and stored URI; packed XML must state guard/exception. Pre-auth authenticates untampered positive payload then positively records tampered selector key-B before exact AuthenticationTagMismatchException; packed callback XML must mark selector untrusted before authentication. EF single/separate positives and same DbContext negative normal named-bus registrations; packed XML must describe one DbContext type per bus. Execute immutable old packages first (expectedthreecausalXMLreds/twoEFpositivegreens), then fresh strictlybuiltpacked candidate, then three onecausepacked-XML counterreversions and rollback. Separate freeze/reviewer for every three-source/consumerdelta; integrated gates pending.

## Package03 EventHub implementation
Research→Plan→Implement inline under existing native signed owner. Install three reviewed fixtures and exactly ten requirement rows; 70 cases. First strict-build and run original closing10,policy28,producer pre-canceled8+positive16 (62 cases); later-canceled8 reserved for corrected end state to avoid deadline-based original proof. Four source fixes: always-await closing with ordered aggregate, validate provider limits and preserve inherited guards/whole-ms timer boundary, copy nine policy properties and options callback for completed ordinary Build, WaitAsync per caller in four deferred paths. Assert exact faults/token/state/route identity/SDK callback/options and normal public DI, release and join all gated operations. Then corrected70,mutants,fresh packages,whole owner,independent frozen adversarial reviewer and final integrated gates. No cloud-start or standalone-lazy-blob closure claimed.

## Package07 plan
Implement three narrow source fixes from hash-verified historical candidates. Author standalone public compiled-DLL oracle after this Research/Plan, strict rebuild both original/corrected tools, assert all parameter tuple results and latency identity/count/completion/timestamp semantics including gated concurrent duplicates. Execute real BDN Dry on corrected source, restore one-cause mutants, native rollback; bind actual compiled tools, packages, lock/cache/runtime and raw BDN reports. Freeze all changes for independent adversarial reviewer. Final whole integrated profiles remain pending.

## Package04 F018 public transaction ownership Plan

Root personally read both bounded NuGet-only consumers, all25 public-route imports and complete factory before authoring. Native canonical36-file EF owner remains untouched/not personally FULL; no whole-owner grade inferred. See evidence PACKAGE04_F018_PLAN.json and ROOT_PUBLIC_READ_CLOSURE. Original16cases precede two-predicate product correction and fresh packed consumer/mutation proof.

2026-10-03T01:12:45.741603+00:00 PACKAGE05 F027/F028/F030: Root personal FULL22 public route sources+six bounded consumer inputs completed; 42-case NuGet-only regression plan bound in evidence/PACKAGE05_PLAN.json before copying/authoring. No canonical Core owner closure or edits claimed.

F018 canonical fixture author delta: Root personally FULL36 owner+57 shared/imports/CI completed before edit; actual SQLite outer transaction and borrowed same DbContext replace fake marker. Expected executionCount1 retained; no additional transaction + same actual ID/reference. Plan evidence PACKAGE04_F018_CANONICAL_FIXTURE_PLAN.json; valid complete UnitArchitecture410 rerun and independent adversary mandatory.


Package05 F030 bus diagnostic supplemental plan.md: see /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE05_F030_DIAGNOSTIC_OWNER_PLAN.json. Eight new isolated NuGet scenarios, typed two-bus same-endpoint grouped/ungrouped, old diagnostic red and valid controls, exact bus identity string; startup PO decision remains open. No canonical Core edits.


Package10 F022 trace owner repair: /Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/PACKAGE10_F022_SOURCE_PLAN.json. Root8sourcesFULL, retain+disposeownActivity on poststart formatterfault in admission and delivery; native public proof plan10cases, real InMemory worker/receiver/store, Fault/Cancellation controls, compiled mutants and independent adversary required.


Package11–16 continuation (current evidence navigation; actual preauthor plans remain immutable in evidence): F004 journal scope Package11; F039 separate root graph Package12; F033 Core Package13; F031 endpoint contributions Package14; F033 SignalR Package15; F019 logger origin/lifetime Package16. Each has its own source/public-owner preauthor plan and complete bounded owner closure, actual selected NuGet consumers, compiled single-effect counterproofs and independent frozen review. No canonical Core766/Abstractions117/Bench15 owner closure transfer. The current ledger/acceptance truth is SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json and each PACKAGE*_ROOT_ACCEPTANCE.json.
Package16 authoritative preauthor Research→Plan: PACKAGE16_F019_SOURCE_PLAN.json and PACKAGE16_F019_PUBLIC_OWNER_PLAN.json; final owner-before-native read R5.32 fresh-process cases cover real Generic Host/default/dynamic/explicit/two owners, messages, exact logging markers, disposal, borrowed identity, sibling flows, fault/pre-cancel/meter controls. Eight one-effect strict variants; store actual tasks, release gates and attempt all cleanup; guard failure is never PASS or universal quiescence. Freeze45inputs/240effects/44receipts/4selectedpackagechains/34Microsoftbindings now awaits final independent review. Only then mark scoped finding repaired; all integrated/API final gates remain pending.

Paket17 F029: Matrixdefault/typed Size/TimeFromFirst/TimeFromLast ±UTC → Matrix; Forcedhandle2 → Matrix; rawequal2 → Matrix; realSystem → SystemSize; firstprovider → ProviderSnapshot; dedup → Dedup; cancelmember/sole → Cancel; primary/cleanup exactorder → Faults; separateelapsed/UTC timerhelper → Helper. GleicheOriginal-/KorrekturAssertions, strikteBuilds/fresh selectedNuget/CoreAbsLockCacheDLL/PDB;3gezielteSingleEffects+positiveControls/baseline/rollback;jedeSource/Publicowner/runnerÄnderung unabhängigesadversarialReview; gemeinsame Abschlussprofile bleiben offen.


F035 package18 planned boundedowner30:22 known-definition controls and8 source-derived endpoint lifetime qualification controls. Ordinary default/typed actualDI InMemory real2business+2terminal; Bind type/equality/lastalias/repeatedlazy; actual disposalcounts/gates/DIdependencyorder/borrowedvalue/exactsolefault. NoNativeyet; source plan choice privateunkeyedowner variants vs unique key actualDIowned definition still research. BeforeNative Rootwholefreshconsumer incllock, independentadversarialeachcodechange, then original/fixed/strictsingleeffect+positive/rollback boundpackages/loadedPE-PDB. Global wholeAPIreview+A+ andcanonicaltests pending.


Package18 F035 R2: 46 bounded public cases per PACKAGE18_F035_PUBLIC_OWNER_PLAN_R2.json; known AddDefinition ownership plus endpoint sibling qualification, caller-owned controls, asynchronous gate/dependency order, default alias baseline and optional disposed-capture. Root whole owner eight files read before any compiler/native behavior gate; lock is pending restore. Previous and corrected receive the same oracle bodies. No canonical Core test owner authority inferred.


Paket19 F034: Research→Plan vor Autorenschaft, PACKAGE19_F034_PREAUTHOR_PLAN.json. Root37 Quellen persönlich FULL/3 eigene exakte Paket18-Reuse;32 geplante öffentliche Fälle, sameNuGet-only owner/oracles, finiteEndpointmultiset vorStart, echte konkrete RoutingSlip-Completion und inverse Kompensation; default/typed/mixed/caller/custom-selector/definition/repeated/cancellation controls. Jede Source-/Owner-/Runneränderung adversarial; Native original/fixed/compiled-singlecause+positive/rollback, dann Freeze+Finalreview. Keine kanonischen Coretests bearbeitet/benotet.

Package19 separate escaped transport candidate:10 public default/typed finite actual topology identity plus canonical/short queue sends. Original/current F034 ownership cases remain unchanged. No timeout failure is a causal qualifier. See PACKAGE19_TRANSPORT_PREAUTHOR_PLAN.json. Root implementation and whole owner reread; independent adversarial preflight before behavior.


Paket19 R4 owner refinement: fresh paired generation retains34 cases; business InputAddress last entity decoded exactly once; definition expected multisets include intentionally separately registered fluent endpoints; full finite callback multisets printed before exact assertions. No source repair credited to these fixture corrections. See PACKAGE19_F034_OWNER_REFINEMENT_PREAUTHOR_PLAN_R4.json. Original R3 owners/packages/evidence preserved.


Paket19 R5: Same41paired publiccases per PACKAGE19_F034_F041_OWNER_PREAUTHOR_PLAN_R5.json and PAired_CAUSALITY_PLAN. F034 baseline uses same final Core/Abs with old Courier only. Eight F041 default/typed divergent success+inversecompensation/literal/aligned controls, public lower configure callback exact captured buscontext+registeredProbe identity and13/3 QoS; strict3/5/6 finite endpointmultisets. Core F04014separatefrozenmatrix remains immutable. Native/regression/singleeffect mutations/freeze/final independent review required.
