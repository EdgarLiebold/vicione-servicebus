# Current review status

This page separates the latest recorded product-wide measurement from older,
chronological test and migration notes. It is a checkpoint, not a claim that
the product is defect-free.

## Current boundary

The API repair baseline is commit `6b451cff07f46c6bf3eee325ff81ead1089e8c89`, merged into `main`.
The completed audit accepted 234 confirmed repairs within their individual recorded boundaries;
it does not claim global A+ quality or production release. The final shared Unit profile recorded
13,460 passing tests across 23 modules, with zero failures or skips. The selected Event Hubs run
recorded 166 passing cases across 22 classes; seven emulator-dependent classes were excluded.
These are dated execution results, not a new run of the continuation work.

The [local completion plan](../.testagent/local-completion-plan.md) schedules fresh-checkout
validation, current coverage, provider checks and the remaining deferred engineering work.
Real cloud validation and ARM64/512-MiB/no-swap device acceptance remain open.

On 7 October the continuation reproduced that unchanged baseline in a fresh clean checkout:
Engineering and Unit locked restores, strict Release builds with zero warnings/errors, and
13,460 native Unit cases over 23 modules with zero failures/skips. The whole-Unit coverage
attempt was interrupted because some non-product test modules do not support the coverage
extension; it is not a passing profile or a current coverage measurement. Local provider execution
has passed its baseline run; independent evidence acceptance is recorded. Fresh-baseline evidence has independent final acceptance.

The bounded Core deadline correction has separate source and native artifact acceptance: 13 fresh
package cases pass, all 7,958 Core cases pass, four compiled single-cause mutants produce the exact
expected finite failures, and an unchanged corrected postcheck passes all 13 cases. These are
functional regression results; they are not a new coverage percentage or full Unit/provider replay.

Separate original-baseline RabbitMQ38 and SQLServer99 local profiles passed with independently
bound runtime/build evidence and own-resource cleanup. These passing baselines do not close the
historical SQL 999/1,000 delivery cause or post-correction provider replay.

The shared Activity stage-A correction has separate source/test and native artifact acceptance:
all eight new cases and all 7,966 Core cases pass; three freshly compiled single-cause mutants
produce five finite expected failures and 19 named healthy results; the unchanged final postcheck
passes all eight cases.

The typed source stage-B1 correction has separate source/test and native artifact acceptance:
both new cases and all 7,968 Core cases pass; three freshly compiled single-cause mutants produce
three finite expected failures and three named healthy results; both unchanged final postchecks
pass. Independent host metrics and meter ownership remain available after ordinary source failure.
The batch handoff stage-B2 correction also has separate source/test and native artifact acceptance:
six new cases and all 7,974 Core cases pass; one freshly compiled handoff reversion produces
three finite expected failures and three named healthy results; six unchanged final postchecks
pass. Actual success, original business failure and context cancellation remain intact.
Process-metric balance stage C1 has separate source/test and native artifact acceptance: eight
new cases and all 7,982 Core cases pass; two compiled guard mutants produce six finite expected
failures and ten named healthy results; all eight unchanged final postchecks pass.
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


These bounded results do not update historical
coverage/CRAP values or qualify a new full Unit/provider/package replay.

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

## Historical product-wide coverage and CRAP checkpoint

The latest complete 33-profile measurement ran against commit
`42a028a7fa8ed6facf941da3d72064cbf438f196` on 30 September 2026. All
receipts name that commit and its `src` tree
`0871a1f1b28cfb4f3303ae8c5fbf7f47010fc6e4`. The measurement covered
all 32 product assemblies and recorded 14,235 passing test executions.

| Measure | Result |
|---|---:|
| Line coverage | 88,032 / 95,297 = 92.3765% |
| Conservative branch coverage | 32,196 / 37,822 = 85.1251% |
| Method identities | 26,317 |
| Methods with CRAP above 30 | 0 |

The [aggregate JSON](quality/t176-aggregate.json) is a byte-for-byte copy of
the local measurement output; SHA-256:
`3b02e44a4229055301a218f2c8e02ce5d7cd90554656db211f1d0fbc64f2a7d2`.
The [33 receipt records](quality/t176-receipts/) preserve their source and
test tree IDs, commands' output hashes, binary hashes, and profile outcomes.
Raw logs, binaries and coverage XML remain local generated artifacts and are
not included in this repository. The recorded result can be independently
checked by rerunning the profiles at the named commit; the compact records
alone do not reproduce the calculation from raw coverage files. Subsequent API repairs changed
the product source tree as well as the tests. The unused legacy-outbox import scripts and their
three unit tests and one local integration test were removed, as were five transition-only
architecture tests. These coverage and CRAP figures describe only the named September commit.
Fresh source-bound coverage is required before making a current-HEAD claim.

## Open reliability finding

The first SQL Server profile timed out in
`ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnceAsync`:
the diagnostic counted 999 distinct arrivals from 1,000 publishes, one
missing application ID, no duplicates, and no remaining delivery in its
normal, error or dead-letter queue snapshot. A fresh-container retry passed
75/75 and is the receipt included in the aggregate. The missing delivery's
cause remains unproven. The successful retry does not close the finding.
The next investigation must distinguish zero routing at publish from loss
before or after the consumer by recording the publish procedure's delivery
count and the subscription, topic and message state for a missing ID.

## API inventory

The [1 October Roslyn inventory](api/roslyn-all-repos-api-2026-10-01.md)
is a dated cross-repository triage snapshot. Its presence-of-XML results do
not establish the semantic quality of every API comment. Sixteen of its
69 projects were flagged by compiler or workspace diagnostics. ServiceBus's
CodeFixes project had a workspace diagnostic, and its packaging-only analyzer
project lacked resolved reference assemblies. The subsequent ServiceBus audit resolved its
compiler/workspace inventory gaps: 33 fresh Roslyn projects had no compiler or workspace/load
errors, with 18,600 source symbols and 833 separately counted accessor representations. This
does not close diagnostics in other repositories or establish semantic correctness from XML
presence alone. Their respective owners retain the cross-repository follow-up.
