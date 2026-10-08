# Local completion plan

Authorized by the Product Owner on 7 October 2026: continue the remaining work autonomously,
with adversarial review of every code change. Device hardware and real cloud resources are
unavailable. Baseline: `6b451cff07f46c6bf3eee325ff81ead1089e8c89`, tree
`63fdebaeeca2442d95bd48c42fb9eaef45768c11`.

The completed API audit remains frozen in `evidence/SERVICEBUS_API_REVIEW_AND_REPAIR/STATE.json` and
the retained review archive. Its 234 accepted repairs do not close deferred engineering work
or unexecuted external evidence. This file schedules execution; `TODO.md` owns deferred work.

| Package | Work | Acceptance / boundary | State |
|---|---|---|---|
| 01 | Fresh-checkout reproducibility; reconcile stale plan/status pages | Locked Engineering and Unit restores, strict Release builds, complete native Unit run, exact modules and raw evidence; independent documentation review | Accepted: fresh 13,460 Unit cases / 23 modules, strict locked builds and independent final evidence review |
| 02 | Current coverage and method risk | Fresh source-bound coverage for current unit/local profiles and portability modes; partial if any required provider is unavailable | Binlog producer/aggregator correction accepted after 10 Python tool contracts and causal negative tests; native measurement pending |
| 03 | Local provider integration and historical SQL missing-delivery investigation | Native provider profiles against isolated local resources, exact message accounting and cleanup; success on retry alone does not close the historical failure | Earlier baseline accepted: 645 / ten modules, RabbitMQ38 and SQLServer99. Current ASB30 functional profile and known-credential log correction accepted; new broker-size anomaly, historical SQL cause and other current provider replays open |
| 04 | Startup validation inventory | All selected static invalid options fail before endpoint start; missing/conflicting/unselected and named multi-bus cases | SQS six Host / 417 SQS / six mutants (15 finite first failures, 21 healthy controls) / final six accepted; Rabbit four Host / 599 Unit / five mutants (19 finite first failures, 41 healthy controls over 60 repeated slots) / restored final twelve accepted; Azure three Host / 607 slots (604 display labels) / five mutants (14 finite failures, 16 healthy controls) / restored six accepted; Core cleanup fixed with four new cases + one control / 8,146 Core / four mutants (six finite failures, 14 healthy controls) / restored five accepted; Core composition three Host / 8,149 Core / four mutants (four finite failures, eight healthy controls over twelve repeated slots) / restored three and exact two-file transfer accepted; Reliable three new Host + ten existing cases / 8,209 Core / five variants (15 first failures, 50 healthy over 65 repeated slots) / restored thirteen and exact four-file transfer accepted; EF four new Host + one existing direct boundary / 436 EF slots (435 labels) / 8,209 affected Core / three variants (six first failures, nine healthy over fifteen repeated slots) / restored five and exact seven-file transfer accepted; further startup and composition paths open |
| 05 | Activity telemetry isolation | Complete emission/lifetime/context inventory, hostile listeners, exact messaging outcome and schema, causal counter-mutations | Shared Activity A, typed source B1, batch handoff B2, process metrics C1, consumer completion C2, delivery clocks C3, snapshots C4, cold sources C5 and public caller cleanup C6 accepted; C6: 38 cases / 8,112 Core / 6 compiled mutations (34 finite failures, 194 healthy controls) / final38; classic EF duration C7 accepted: 13 SQLite / 432 EF / 5 compiled mutations (19 finite failures, 46 healthy controls) / final13; broader schema and aggregate closure open |
| 06 | Cache observer/index and deterministic time TODO reconciliation | Inspect current owners and prior accepted implementation before altering code; close only demonstrated contracts, implement genuine remaining gaps | Three Core deadlines accepted with 13 contracts / 7,958 Core / four mutants; cache comparer/retirement accepted with 30 contracts / 142 Cache / 8,142 Core / eight mutants (12 finite failures, 228 healthy controls) / unchanged final30; observer failure-channel 57 new / 59 focused / 8,206 Core (199 Cache) / three mutants (117 finite failures, 60 healthy controls over 177 repeated slots) / restored59 and exact three-file transfer accepted; retirement/dispatch four Facts / 8,213 Core (203 Cache) / four variants (five finite failures, eleven healthy over sixteen repeated slots) / restored four and exact two-file transfer accepted; Azure receiver/session clock six cases / 613 Azure native slots (610 labels) / four variants (four finite failures, twenty healthy over 24 repeated slots) / restored six, exact runtime18 bytes and independently reviewed six-file transfer accepted; SQS topic lifecycle eleven cases / 428 SQS / seven controls (fourteen first finite failures, sixty-three healthy over77 repeated slots) / restored eleven and exact runtime15 bytes / independently reviewed four-file transfer accepted; Event Hubs selected checkpoint clock two new Facts / six-case SPI replay / whole196 native slots (194 labels) / five controls (five first finite failures, five healthy over ten repeated slots) / forced post two and exact runtime16 / independently reviewed ten-file transfer accepted; SQS/SNS captured batch clock nine cases / whole437 unique / six controls (nineteen first finite failures, seven healthy over26 repeated slots) / forced post nine and exact runtime15 / independently reviewed eleven-file transfer accepted; SQS visibility caller and broader observer/resource/adapter contracts open |
| 07 | Package-only developer journeys and local benchmarks | Execute supported journeys and real local infrastructure scenarios with full message counts; no target-device performance claim | Pending |
| 08 | CI qualification | Review workflow against the accepted local commands and evidence; retain operational disable until the local acceptance prerequisite is met | Pending |
| External | Azure/AWS/managed broker/provider contracts and S3 product decision | Real cloud/account evidence unavailable; arbitrary S3 TTL exposure requires an explicit complete product contract | Waiting for prerequisites |
| Hardware | ARM64, 512 MiB, no swap, sustained load and recovery budgets | Requires the actual target device | Waiting for device |

The bounded cache receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_COMPARER_RETIREMENT_ACCEPTANCE_R1.json`. Separate child events are never added
to parent counts; observer order/failure channels/backpressure and measured memory remain open.
The bounded SQS receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_SQS_STARTUP_STAGE_A_ACCEPTANCE_R1.json`.
Its positive cases stop at an exact pre-runtime sentinel; other startup owners and their
multi-bus paths, plus actual healthy full-host startup, remain next. The scoped additive generic SQS
API comparison is accepted; the global packed API baseline remains open.

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

Per code package: complete source/test-owner read → Research/Plan → causal original regression
where applicable → minimal correction → meaningful native tests and one-cause counterproofs →
hash-bound freeze → separate read-only adversarial review → resolve findings → affected aggregate
checks. Product writes and .NET/provider processes are serialized. Historical receipts are never
rewritten or relabelled as current runs. No real cloud resources are created.

Package 01 uses a separate clean local Git checkout and an empty artifacts directory. Its package
cache may be reused, but no prior compiled outputs or test results may enter the new result.
Record sandbox/network failures honestly and rerun the same locked command with only the required
execution permission. Capture a distinct binary log for every actual MSBuild invocation.

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
