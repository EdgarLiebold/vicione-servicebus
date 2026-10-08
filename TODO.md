# Deferred engineering work

This file contains bounded work that was deliberately kept out of the active implementation slice.
It is not a second feature catalog, architecture, changelog, or license record. An item is removed
only after its acceptance evidence is committed.

## Verify startup validation across retained capabilities

The historical V5 API review identified delayed validation of correctness-critical options
(`API-005`). Its specific payload-admission example is fixed in the current source, and the
RabbitMQ, Azure Service Bus and Amazon SQS registrations now call `ValidateOnStart()`.
Those examples do not establish that every retained provider, persistence, scheduler and
consumer registration fails at host startup for invalid configuration.

Inventory the current options registrations and their validation paths, including named
multi-bus options. For each correctness-critical option, prove that an invalid selected
configuration fails before a receive endpoint starts, with a useful bus and feature name
in the error. Include negative tests for missing, malformed and conflicting settings and
verify that an unselected optional capability does not fail startup. Use the current source
and public API as the authority; the V5 counts and proposed API shapes in the historical
review are not current requirements. The original review is preserved in Git branch
`archive/review-v5-handover-20261001`.

The current inventory is already recorded in `docs/static-configuration-validation.json`:
43 public declarations, including one abstract base and one validator, and 41 concrete models at 20 host-start,
11 materialization, eight construction and two registration boundaries. The remaining work is
registration-path evidence, including selected and unselected named/multi-bus owners. Direct
validator tests do not establish that a real host rejected configuration before endpoint start.
The accepted batch timer-range guard runs at the actual context-provider boundary; it is not a
general startup-validation proof.

The bounded named SQS stage A now proves six actual Generic Host cases for selected Region,
Scope and Scope/Region validation, selected default behavior and unused malformed-default
isolation. All six pass, as do the 417 local SQS cases; six compiled single-cause mutations
produce 15 finite first failures with 21 healthy controls, followed by six unchanged green
postchecks. The positive cases stop at a chosen sentinel before provider runtime; full runtime
startup and other named/multi-bus owners remain open. Acceptance:
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package04-startup/ROOT_SQS_STARTUP_STAGE_A_ACCEPTANCE_R1.json`.

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

Other startup owners and models, further composition cases and provider full runtime startup remain open. Current coverage, aggregate Unit/provider replay, adapters, cache observer contracts,
telemetry schema, developer journeys, benchmarks, CI, cloud and target hardware are separate work.
The new ASB broker-size invariant finding stays open. The frozen 234 accepted audit repairs and
all 62 external obligation IDs remain unchanged; no global A+ or release acceptance is claimed.

## Complete Azure Service Bus validation against the real cloud service

The local Azure Service Bus emulator executes queue, topic, subscription, rule, forwarding,
session-state, scheduling, duplicate-detection and dead-letter contracts. It does not provide a
truthful substitute for Azure-owned identity, tier, availability, partition, transport and clock
semantics. The 46 open obligation IDs are:

`OBL-R0-CLOUD-0005`, `OBL-R0-CLOUD-0019`, `OBL-R0-CLOUD-0020`, `OBL-R0-CLOUD-0021`,
`OBL-R0-CLOUD-0025`, `OBL-R0-CLOUD-0026`, `OBL-R0-CLOUD-0027`, `OBL-R0-CLOUD-0028`,
`OBL-R0-CLOUD-0044`, `OBL-R0-CLOUD-0057`, `OBL-R0-CLOUD-0058`, `OBL-R0-CLOUD-0059`,
`OBL-R0-CLOUD-0066`, `OBL-R0-CLOUD-0067`, `OBL-R0-CLOUD-0068`, `OBL-R0-CLOUD-0069`,
`OBL-R0-CLOUD-0072`, `OBL-R0-CLOUD-0079`, `OBL-R0-CLOUD-0080`, `OBL-R0-CLOUD-0086`,
`OBL-R0-CLOUD-0088`, `OBL-R0-CLOUD-0089`, `OBL-R0-CLOUD-0095`, `OBL-R0-CLOUD-0096`,
`OBL-R0-CLOUD-0098`, `OBL-R0-CLOUD-0099`, `OBL-R0-CLOUD-0105`, `OBL-R0-CLOUD-0108`,
`OBL-R0-CLOUD-0109`, `OBL-R0-CLOUD-0117`, `OBL-R0-CLOUD-0122`, `OBL-R0-CLOUD-0124`,
`OBL-R0-CLOUD-0125`, `OBL-R0-CLOUD-0130`, `OBL-R0-CLOUD-0131`, `OBL-R0-CLOUD-0132`,
`OBL-R0-CLOUD-0137`, `OBL-R0-CLOUD-0138`, `OBL-R0-CLOUD-0247`, `OBL-R0-CLOUD-0248`,
`OBL-R0-CLOUD-0250`, `OBL-R0-CLOUD-0251`, `OBL-R0-CLOUD-0254`, `OBL-R0-CLOUD-0255`,
`OBL-R0-CLOUD-0256`, `OBL-R0-CLOUD-0257`.

Keep them open until a short-lived real namespace runs them. Their historical
transition mapping is available in Git commit `42a028a7fa8ed6facf941da3d72064cbf438f196`.

The external profile must use run-scoped entities and credentials, bind the exact Azure resource and
SDK versions, and prove both positive and negative outcomes for Entra/RBAC and SAS validity,
partitioned queue and subscription delivery, Premium versus Standard message-size limits,
AMQP-over-WebSockets, broker-clock AutoDeleteOnIdle, real retry backoff, entity deletion and outage
recovery, lock/session-lock renewal and loss, and TTL/lock-expiry exception classification. It must
use provider state or service acknowledgements as terminal barriers, never sleeps, polling success,
shared resources or a local-emulator result presented as cloud evidence.

The current local emulator replay passes all 30 native cases with independently bound build,
runtime and own-resource cleanup evidence. Its credential-log finding is repaired and separately
postchecked. These local results close none of the cloud obligations above. The new local finding
`ASB-BROKER-INVARIANT-01` still needs a cause-qualified investigation: three negative entity-size
records occur during passed dead-letter cases. Isolate those cases with a normal Complete control
against the pinned local emulator, retain exact entity/operation evidence and determine whether
an SDK, product or emulator response is required. Test success alone does not close the finding.
The diagnostic inventory also retains partially correlated post-deletion/session/link errors,
an unmatched internal missing entity and the emulator's disabled Server GC configuration.
Evidence: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package03-infrastructure/asb-current-native-r3/ROOT_CURRENT_ASB30_FUNCTIONAL_ACCEPTANCE_R1.json`.

The same profile owns the remaining management-plane and namespace-boundary contracts: topology-only
deployment, dynamic and multi-bus subscription endpoints, unchanged-subscription idempotence,
high-entity-count restart, cross-scope routing, temporary endpoint lifetime, scheduled publish and
send-context enqueue, raw-JSON response correlation, prefetched-message shutdown, shutdown-grace
publishing, complete broker-assigned message context values, and the combined Azure Blob Storage plus
Service Bus message-data matrix. These rows stay pending until the real service produces the named
positive and negative outcomes; their retired inherited tests are not reported as executing evidence.

## Separate cache index projection from external observation

`ResourceCache<T>` has replaced the historical `GreenCache<T>` owner. Key preparation, index commit
and index removal are separate from optional observer callbacks. Callbacks are serialized and
awaited without an internal notification queue; failure logging is best effort, complete fan-out is preserved,
and mutating observer reentrancy is rejected. The original implementation evidence is retained at
commit `cf2e9ebfbc193573fd376df549469a956bc7de80`,
`evidence/WP-F2-SERVICEBUS-REVIEW-INTEGRATION-01/V4-RESOURCE-CACHE/VALIDATION.md`.

Ordinary comparer/publication and retired-resource failures are now repaired through prepared
slots, atomic publication and callback-free exact-owner unlink. Failed bulk-index preparation,
colliding head/middle/tail removal, late old factories and partial expiry-clock failures have
bounded native proof: 30 focused / 142 Cache / 8,142 Core passing cases, eight compiled
one-cause mutants with 12 finite failures and 228 healthy controls, and 30 unchanged postchecks.
See the [bounded cache repair receipt](evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-cache-consistency/ROOT_CACHE_COMPARER_RETIREMENT_ACCEPTANCE_R1.json).

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

Beyond these four cases, broader concurrent add/remove/clear ordering and bounded caller/waiter
memory under backpressure remain open. Serialized callbacks alone do not prove FIFO state-commit
order or fairness. Preserve complete observer fan-out, mutating-reentrancy rejection and index
generation guards; do not add an unbounded single-drainer queue.
The inherited per-lookup expiry snapshot allocation still needs a local benchmark; no measured
heap/RSS or device-memory acceptance follows from the comparator/retirement tests.
Acceptance requires hostile key providers, blocking/throwing/reentrant observers, concurrent
add/remove/clear, bounded memory under backpressure, exact index consistency, and one-cause
mutations for ordering, failure isolation and queue bounds.

## Continue deterministic time normalization beyond envelope materialization

Envelope materialization is complete: System.Text.Json and MessagePack now consume one internal
serializer-independent projection for identifiers, addresses, headers, message types, host data and
times. A standard .NET `TimeProvider` is attached once to the pipeline context, each projection uses
at most one UTC snapshot, and zero or negative TTL values are materialized exactly without a
serializer-owned grace period. Payload encoding remains owned by each serializer. Target-contract
tests, cross-format comparison and four effective clock/clamp/drift mutations protect the rule.

The separate send-context projection in `ForwardMessagePipe<T>` remains intentional: transports,
observers, middleware and caller pipes need that state before body serialization. Do not merge these
causally different stages merely because they carry some of the same values.

The generic-forwarding policy is already resolved and implemented: after the caller pipe, only a
positive TTL can revive an expired inherited envelope; otherwise the forward is logged as
`FORWARD-EXPIRED` and discarded before transport dispatch, persistent-outbox storage or mediator
dispatch. This is intentionally different from the one-second response/fault grace. This TODO must
not reopen that policy or move it back into individual serializers or transports.

Testing observation primitives are also complete. `AsyncElementList<T>` and
`AsyncInactivityObserver` now use one injected standard `TimeProvider`, existing constructors retain
their `TimeProvider.System` defaults, synchronous callbacks execute outside the list monitor, and
source-query failures remain observable. Their native fake-time, cancellation, concurrency and
mutation proofs must be replayed but not redesigned by this TODO.

Recorded-message timestamps are complete as well. Sent, published and received observations, their
lists, bus observers, consumer/saga/handler registrations and mediator harness now derive their
immutable snapshots from the harness `TimeProvider`. Exact metadata, typed and untyped success,
consume faults and independent process-clock regressions are covered by native tests.

Harness budgets, `RollingTimer`, `InactivityTestObserver`, saga polling, job-service intervals,
copy-context TTL and request timers already use the standard provider model. Historical bounded
runtime-time validation and mutations are retained at commit
`0af45942509b6391a8265cc2eb366b422714e375`,
`evidence/WP-F2-SERVICEBUS-REVIEW-INTEGRATION-01/V4-RUNTIME-TIME-OWNERSHIP/VALIDATION.md`.

The 7 October bounded Core package corrects `OutboxMessagePipe` delivery and `ConsumerAgent`
unexpected-loop stop and consumer grace. They use the context provider, and outbox delivery keeps
its single operation clock across asynchronous loading. Its accepted receipt records 13 fresh
package cases, all 7,958 Core cases, four compiled single-cause mutants and a final 13-case green
check: `evidence/WP-SB-LOCAL-COMPLETION-20261007/package06-core-deadlines/ROOT_CORE_DEADLINE_ACCEPTANCE_R1.json`.
Affected complete Unit/provider replay remains a separate aggregate gate.

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

The SQS visibility-renewal caller is a concrete source-inventory follow-up; it still requires
complete caller/lifecycle qualification and matched native proof before any separate repair.
Continue the remaining timer inventory and affected adapter replays. This worklist is not a
claim that every other operative timeout is covered. Preserve domain-owned timestamp
providers where they are actual public behavior; replace accidental process-clock reads with the
same standard context/provider model only after every caller and persisted/wire consequence is
understood. Acceptance requires deterministic boundary tests, RabbitMQ and every retained affected
adapter, public-API/package comparison, and targeted one-cause time mutations. It also replays the
completed envelope and forwarding contracts without redefining them.

## Complete external benchmark scenarios

The transport- and SQL-Server-backed benchmark scenarios remain tracked in
[`benchmarks/ToDo.md`](benchmarks/ToDo.md). Complete them only with their real infrastructure and do
not replace them with inventory-only or skipped green results.

## Make Activity telemetry observationally no-throw

The bounded shared Activity stage A is accepted: ordinary `CurrentChanged` failures are isolated
in shared restore/cleanup, stopped captured parents resolve to the nearest live ancestor, and
exception-event preparation preserves the original error and independently records Error status.
Its receipt records eight new cases, 7,966 Core cases, three compiled single-cause mutants and
eight green unchanged postchecks:
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_A_ACCEPTANCE_R1.json`.
Typed instrumentation source construction is also accepted in the bounded stage-B1 receipt:
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_B1_ACCEPTANCE_R1.json`.
An ordinary source failure preserves independent actual host metrics and their ownership. Both new
cases and all 7,968 Core cases pass; three compiled one-cause mutants fail finitely with healthy
controls, followed by both unchanged green postchecks.
Batch handoff stage B2 is accepted in its bounded receipt:
`evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_B2_ACCEPTANCE_R1.json`.
The actual pipe preserves success, original business failure and context cancellation after an
ordinary handoff callback error. Six new cases and all 7,974 Core cases pass; one compiled
reversion yields three finite failures with three healthy controls, followed by six green
unchanged postchecks. This does not add an explicit finally-restoration contract.
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
Current coverage, full Unit/provider replay, cache/startup/adapters, CI, real cloud and hardware
remain open.


Recorded optional preparation and public caller cleanup stages A through C7 are accepted above. Process-metric balance stage C1 is accepted: eight new cases, 7,982 Core cases, two compiled
guard mutants (six finite failures / ten healthy controls) and eight unchanged green postchecks.
The bounded receipt is `evidence/WP-SB-LOCAL-COMPLETION-20261007/package05-activity-telemetry/ROOT_STAGE_C1_ACCEPTANCE_R1.json`. This TODO remains open until the complete emitted-schema inventory and affected aggregate gates
are accepted.
A throwing or otherwise hostile listener must never change message delivery, persistence,
retry, scheduling, outbox or transport semantics. Do not patch only the Entity Framework outbox
path; that would create inconsistent telemetry behavior and a second policy.

Run one path-complete observability slice that inventories every activity source, listener callback,
`StartedActivity` notification and activity-lifetime owner. Introduce one explicit no-throw
observation boundary while preserving the current OpenTelemetry source names, tags, status,
parentage and timing. Product exceptions and cancellation must retain their original identity, and
listener failures must not be converted into retries, faults or delivery failures.

Acceptance requires deterministic tests with throwing activity listeners at every distinct
emission owner, one-cause mutations that remove the isolation, exact OpenTelemetry schema checks,
all affected unfiltered native profiles, and zero behavior or public-API loss.

## Complete MessageJournal external provider validation

The current provider contract is covered hermetically and against ephemeral PostgreSQL and Azurite
instances. Before release, run the same bounded append, retention, concurrency, failure-isolation
and public-composition contract against short-lived real SQL Server, Azure SQL and Azure Table
resources in the `External` profile. Do not represent an emulator result as proof of a cloud
service's transaction, concurrency or storage-limit behavior, and do not weaken the provider set to
avoid external infrastructure.

Acceptance requires unfiltered native xUnit/MTP execution, zero skips, isolated per-run resources,
secret-free durable evidence, exact resource cleanup and a documented disposition of every semantic
difference from the local PostgreSQL/Azurite results.

## Complete Azure Table persistence validation against the real cloud service

The native Azure Table cohort executes the full repository path locally against a run-scoped
Azurite table: entity conversion, validated keys, saga insertion/read-only/update, real ETag
conflicts and retries, durable futures (including routing slips), all three job-service sagas, and
the bounded optional MessageJournal provider. This is the required local integration proof, but it
does not prove Azure account authentication, cloud networking, production throttling, service-side
limits or every behavioral difference between Azurite and Azure Table Storage.

Before release, add one source-mirrored xUnit 4 / Microsoft Testing Platform 2 `External` project
that runs the same provider-crossing contract against a short-lived real Azure Table resource. The
selected Azure group must fail before discovery when configuration is incomplete; it must never
skip or silently fall back to Azurite. Use the existing typed configuration pipeline and the
`Azure.Identity` credential chain, create a unique table per test run, avoid logging secrets or
tokens, and delete only the exact run-owned tables during cleanup.

The External matrix owns three distinct, non-interchangeable service contracts from the frozen R0
ledger. `OBL-R0-PER-0455` exercises Cosmos DB for Table separately, including its consistency model,
property differences and causal 429/retry-after response. `OBL-R0-PER-0456` proves Entra ID success,
insufficient-scope failure and token refresh during an in-flight operation. `OBL-R0-PER-0457`
verifies the real service response at the 1-MiB entity, 255-property and maximum-property-size
boundaries. Azure Table Storage success must not be reported as Cosmos compatibility, and neither
service may fall back to Azurite.

Acceptance requires locked Release restore/build, unfiltered External execution with zero failures
and skips, resource-cleanup evidence, exact Azure SDK/service error identities for authentication,
throttling and optimistic-concurrency failures, and an explicit disposition of every observed
Azurite-versus-Azure difference. Local Azurite results must remain labelled `LocalIntegration` and
must never be promoted into this cloud verdict.

## Complete Entity Framework SQL Server and Azure SQL external variant validation

The inherited Entity Framework R0 set contains 90 obligation contracts but 156 execution variants.
Current native coverage executes or provider-neutrally consolidates 116 of those identities: 48
unparameterized, 39 provider-neutral and 29 against real PostgreSQL. The remaining 40 identities are
provider-specific: 29 SQL Server cases and 11 SQL Server resiliency cases. They remain
`EXTERNAL_PENDING`; neither their former execution nor a local PostgreSQL result is evidence that
SQL Server or Azure SQL behaved correctly.

Build one source-mirrored xUnit 4 / Microsoft Testing Platform 2 External cohort for these 40
identities. Reuse one behavioral contract where SQL Server and Azure SQL are semantically identical,
but execute it against both short-lived real SQL Server and Azure SQL resources wherever deployment
or retry behavior can differ. The profile must fail before discovery when its selected provider is
not completely configured; it must never skip, inventory, or silently downgrade a missing resource.
Resource coordinates come from the one typed native-test configuration pipeline. Credentials stay
in the shared User Secrets store or the provider credential chain and never enter source, logs or
durable evidence.

Acceptance requires an exact 156-row variant-disposition matrix derived from the frozen R0 set,
locked Release restore/build, unfiltered External execution with zero failures and skips, run-scoped
database names, cleanup evidence, one-cause retry/transaction/provider mutations and independent
product and test/evidence PASS reviews. Only then may the 40 rows move from `EXTERNAL_PENDING` to an
executing terminal disposition.

## AWS real-service closure

The native AWS cohort uses one run-scoped LocalStack fixture to prove actual AWS SDK request,
serialization, persistence, concurrency and lifecycle-composition paths for SQS/SNS, DynamoDB and
S3. It does not claim IAM/OIDC, quota, throttling or service-controlled delayed behavior from an
emulator. The following frozen obligations therefore remain release-blocking `EXTERNAL_PENDING`:

- `OBL-R0-CLOUD-0216` and `OBL-R0-CLOUD-0217`: real long-running SQS visibility renewal and
  isolation between slow consumers;
- `OBL-R0-CLOUD-0265`: the default AWS SDK credential chain, short-lived OIDC credentials and
  in-flight refresh;
- `OBL-R0-CLOUD-0266`, `OBL-R0-CLOUD-0268` and `OBL-R0-CLOUD-0272`: real SQS/SNS payload and
  attribute quotas, long polling/cancellation and partial batch-failure semantics;
- `OBL-R0-PER-0523` and `OBL-R0-PER-0524`: service-controlled DynamoDB TTL deletion and provisioned
  throughput throttling, distinct from optimistic concurrency;
- `OBL-R0-PER-0603`: service-controlled S3 lifecycle deletion after the configured retention.

Run these in source-mirrored xUnit 4 / Microsoft Testing Platform 2 External projects against a
short-lived real AWS account boundary. Configuration must fail before discovery when the selected
role, region or resource owner is incomplete; tests must never skip or fall back to LocalStack.
Use OIDC and the normal AWS SDK provider chain, create only run-owned resources, persist no access
key, secret, session token or signed URL, and delete the exact run-owned resources even after a
failed test. Acceptance requires zero failure/skip, exact provider error identities, bounded causal
barriers instead of wall-clock sleeps, resource-cleanup evidence and an explicit LocalStack-versus-
AWS disposition for every observed semantic difference.

S3 arbitrary per-message retention is a separate release-blocking product decision. The current
repository deliberately accepts either no TTL or a positive whole-day TTL exactly equal to its
immutable bucket lifecycle policy; it rejects every value that would otherwise promise unsupported
per-object deletion. Before exposing arbitrary TTL, choose and implement one complete ownership
model (for example object tags plus owned lifecycle rules with bounded policy cardinality, or a
separate expiration index and deletion worker). Prove concurrent policy updates, restart recovery,
partial failure, cancellation, exact object retention, bounded resource growth and real AWS
eventual deletion. Do not silently round durations, create one lifecycle rule per object, or accept
a TTL that the product cannot enforce.

## Complete Event Hubs validation against the real cloud service

The pinned local Event Hubs emulator and Azurite own deterministic transport, checkpoint, retry,
metadata, saga and lifecycle verification. They do not issue Microsoft Entra tokens, evaluate Azure
RBAC, reproduce service-controlled partition rebalancing under real namespace pressure or enforce the
real service's retention lifecycle. Those differences remain visible instead of being counted green:

- `OBL-R0-CLOUD-0258`: workload identity, authorization denial and token refresh against Azure;
- `OBL-R0-CLOUD-0261`: service-owned partition rebalance with exact checkpoint continuity;
- `OBL-R0-CLOUD-0262`: service-enforced retention at the configured boundary.

Run these in a disposable Azure namespace with OIDC/workload identity and no stored client secret.
Acceptance requires bounded provider-owned state and delivery barriers, exact partition/checkpoint
continuity, explicit cleanup of the namespace resources, zero skip/failure and evidence that names the
Azure endpoint and identity mode without recording tokens or connection strings.

## Complete RabbitMQ validation against Amazon MQ

The pinned run-scoped RabbitMQ fixture owns every deterministic protocol, topology, retry, lifecycle,
stream, scheduling and management assertion. `OBL-R0-BRK-0004` remains external because it requires
Amazon MQ's managed TLS endpoint, AWS-owned certificate chain, account policy and service lifecycle;
counting a local container as that proof would be false.

Run the retained connection/start/stop case against a disposable Amazon MQ RabbitMQ broker through
short-lived AWS identity, without storing credentials or connection strings. Acceptance requires an
exact successful endpoint ready/stop lifecycle, bounded provider state, explicit broker/resource
cleanup, zero skip/failure and evidence that identifies the AWS region and authentication mode without
recording secrets.
