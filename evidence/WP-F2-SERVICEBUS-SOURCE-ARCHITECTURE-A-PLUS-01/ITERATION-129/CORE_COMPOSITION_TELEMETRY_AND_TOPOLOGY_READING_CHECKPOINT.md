# Iteration 129 — Core composition, telemetry and topology reading checkpoint

## Outcome and claim boundary

The main agent personally completed all 59 remaining files of the connected Core
contract-review packet: 14,429 physical lines, 338 declared test methods and 398
historical native cases. Every file was read visibly through EOF, including
fixtures, members, arrangements, assertions and comments. No generator authored
code, comments, tests or this report. Read-only inventories bind already completed
personal reads; they neither replace reading nor produce review judgments.

The connected packet is now 102/102 files: 23,480 lines, 508 declarations and 665
historical cases. Cumulative Core owner reading is 314/557 files / 87,830 lines;
243 owning inputs remain. A declaration/name reconciliation is not a complete C#
parser, effective build-graph review, full GitReadSet or whole-owner admission.
Agreement §4.3 therefore still prohibits new Core test design/editing and full
owner acceptance. The criteria below are deferred review dispositions, not newly
designed tests, executed counter-mutants or confirmed production defects.

This is an intermediate reading/review checkpoint. It does not complete the
original whole-product A+ goal or certify any productive area as A+. No source,
test, project, dependency or productive comment was changed in iteration 129.
No fresh build, native execution, coverage, CRAP, mutation, global Async audit or
cloud/provider acceptance was run. Existing passing receipts remain historical.

## Secured input, authority and scope

Input HEAD is `020c146f8067d8f5bf38ef51aa35344e7dd7709e`, previously normally
committed, annotated-tagged, atomically pushed and independently verified by
branch/tag/peeled refs in iteration 128. Its parent is
`6c0916b20eb5b8f71ca924c84b87b3bca054f50e`.
Source tree: `400a506421aa680730469d7bfbb4b78e364164b7`.
Core owner tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.

Selected authority remains `WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`.
The previously fully read agreement, glossary, current README, current order,
findings and selected Slice retain their exact hashes. DECISIONS alone changes
from `43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4`
to `72d5f1b02a4b81f3b70c3699b2878768eec78eafda253be90df567c1858ce285`.
The new, fully read PO-2026-09-15-03 table row and whole section concern Licensing
EF Core, standard Microsoft packages and SQLitePCLRaw/native SQLite approval.
Removing exactly that row and section reconstructs the prior file byte-for-byte
by SHA256; decision count remains 116. ServiceBus authority is unchanged. The
Licensing-only exception does not authorize a different ServiceBus workflow.

The independent authority/input diagnostic actually exits 0, checks all seven
bindings, the exact prior DECISIONS reconstruction, input/source/Core Git trees,
and unchanged bytes of the three owned history files before prefix insertion.
Earlier failed reconstruction probes and the JavaScript quoting error are not
successful authority receipts; no authority file was edited.

The first handwritten-manifest diagnostic exits 1 before validation: system Ruby
does not support Array.filter_map. The corrected read-only diagnostic uses
map/compact with the same strict gates; this is tooling compatibility, not a
productive-code or compiler failure. Preserve the failed attempt separately.
The next diagnostic exits 1 because its table parser drops the final hash field
when Ruby split removes a trailing empty column. Preserve trailing columns with
split("|", -1); keep exact five-column comparison and all Git/hash/count gates.
Git's new-file no-index diff exits 1 with no whitespace diagnostic because the
file differs from /dev/null; that is not a successful zero-exit validation receipt.
The scoped existing-file whitespace check exits 0; the strict validator also
checks the new report's whitespace explicitly.

Exact write scope is this report plus `.testagent/plan.md`, `.testagent/research.md`
and `.testagent/status.md`. Their old tails must remain byte-identical to input
HEAD. No product `review/`, `TestResults/`, workspace legacy repository or unrelated
Licensing/Editor/architecture work is read, enumerated, edited or staged by this
checkpoint. No internal or external counter-review is claimed for iteration 129.
The unadmitted iteration-128 internal advisor retains its documented violations
and supplies no independent-acceptance credit here.

## Personally read contract observations

The following observations cover the complete files in the manifest, not only
their named tests. Positive counterexamples matter: do not turn a narrow gap into
a blanket assertion that adjacent tests lack the same proof.

### Dependency injection — nine files / 3,322 lines

`DependencyInjectionConfigurationContractTests` verifies precise parameter names,
typed/untyped registration boundaries, canonical scoped consumer descriptors,
definition-instance identity, true foreign bus/rider owners, idempotent completion,
late rejection, hidden implementation visibility, null-factory resolution and
original specification failures. The rejected open-consumer path preserves an
empty service collection, and missing specifications prevent callbacks. Other
negative calls do not always inspect retained state. Combined definitions use
equal QoS values, mixed ordering is not positively distinguished, compensation
filter checks are count-only, and completion does not always pair the configurator
identity. Validation yields no failures in the successful fixture, so it does not
establish disposition-before-configuration. Some inline providers are not disposed.

`EndpointConfigurationTests` asserts real probe shape, distinct bus/endpoint QoS
values, default/registration precedence, validated DI ownership and stopped
health. Shared consumers reject conflicting QoS before callbacks. Identical
shared-owner values cannot distinguish merge policy; probe values do not prove
actual processing concurrency. One success-tail shutdown lacks failure cleanup.
`EndpointRegistrationTests` precisely checks real/wrapper ownership in both
directions and null parameters; this bounded matrix is positive evidence, not a
new gap merely because external owner mutation is unarranged.

`GenericRequestClientTests` forwards exact message/value/timeout/token/options,
returns the same handle/task and leaves the unused proxy untouched. Name-only
proxy dispatch cannot distinguish every same-named typed/object overload. Its
helper disposes the DI provider before returning the wrapper; pending-operation
and real scoped ownership are not established by completed tasks.
`HandlerRegistrationTests` checks 16 reflected invalid calls leave descriptor
count unchanged, and real handler/request overloads produce known bodies, exact
delivery shape and terminal drained record counts. Scope IDs are nonempty/unique,
but not paired to expected owner/slot/instance/disposal. Some request/conversation
IDs are compared only to each other. Startup precedes protected shutdown.

`MultiBusRequestTests` proves default, custom and dynamic bus clients, known
correlations, bus identities, addresses and cross-bus response headers. Secondary
delivery for the same consumer and definition-created buses is not exercised;
callback counts cannot detect swapped owners. First-signal consumers do not prove
terminal uniqueness, and rejected duplicate registration does not inspect ghost
or retained entries. `MultiBusScopeIsolationTests` has actual owner markers and
foreign-provider rejection in both directions with distinct providers/setters.
Separate directions do not prove overlapping/nested scopes or post-completion
leak freedom. Sequential cleanup can skip the second bus after failure.

`ReceiveEndpointDependencyTests` holds readiness, observes nonblocking host start,
then releases the dependency and consumes the known message with terminal count
and exception checks. An immediate incomplete-task sample is not an attempted
receive barrier; aggregate unhealthy status does not identify its cause. Some
waits lack a local operation bound. `ScopedPipelineTests` proves actual scope
provider and marker identity, separation from root, real known responses and one
excluded-interface header/invocation. It does not pair disposal/terminal uniqueness
or an included-interface control. The existing identity proof is strong.

### Diagnostics and events — six files / 1,487 lines

`MessageDiagnosticRedactorTests` covers exact sensitivity scopes, inheritance,
fields, visible controls, bounded Unicode/control handling, literal primitive
formats, normalization, DI override and hostile ToString avoidance. A culture
invariance test never changes culture; long URI input is unarranged. The
collectible-type test does not explicitly keep the inspector/cache owner alive
through collection: owner collection can mask a strong cached Type reference.
Sensitivity and hostile conversion are not combined in one arrangement.

`BusReadyEventTests` establishes host/bus reference identity and exact null names.
`HostReadyEventTests` proves detached endpoint/rider arrays and read-only mutation
rejection, but never asserts the supplied host address; multi-element ordering and
empty acceptance are unarranged. `ReceiveLifecycleEventTests` observes transport
bits, addresses, failures and metric snapshots, including both transport flag
values and mutable-source changes. Endpoint wrapper flags use only one value per
event; underlying-event mutation and boundary metric values are not arranged.

`FaultExceptionInfoTests` establishes constructor/inner identity, detached data
and URN arrays, known time/IDs, real fault scalar transport, ordered aggregate
limits, nested depth limits, bounded keys/values and hostile getter/enumerator
handling. The four bounded diagnostic text fields share the same character and
are checked only for length, so swapped or corrupted projections can survive.
Explicit-info middle entries, ordinary remote stack/source and partial enumeration
failure are less exact than the aggregate/scalar controls. The fault helper takes
its single-record snapshot before stopping the bus and does not pair the original
message ID/body. Nonstring-key omission lacks an exact key-set check.

`PolymorphicFaultDispatchTests` exercises five real base/interface shapes and
known fault correlations/types/URNs with terminal publication singles. Typed fault
consumers expose first signals, not terminal delivery counts, and required/forbidden
base URNs are not fully distinguished by Contains assertions.

### Reflection — four files / 765 lines

`DynamicImplementationBuilderTests` exercises actual generated property values,
inherited contracts, complete attribute payloads, init-only metadata, shared
property implementation, stable parallel type identity and precise bad shapes.
Rejected emission has no retained side-effect observation; Parallel.For lacks a
bound or forced overlap. Generated bus metadata is not execution of its
constructor/control properties; IsCollectible is not an unloading proof.
`MessageImplementationCacheTests` supplies typed/untyped identity, assignability
and old-export absence, complemented by adjacent actual property execution.

`PropertyCacheContractTests` verifies stable metadata, real generated target
read/write, exact missing/mismatched behavior and owner guards. One property slot
cannot establish cross-slot isolation; invalid-name exception subtype is broad.
`ReadWritePropertyTests` actually invokes public/private accessors and preserves
the original accessor failure with precise shape/runtime guards. It bypasses the
cache, so it does not close that cache-specific gap.

### Logging — three files / 738 lines

`LogContextTests` checks exact categories, levels, nine arities, formatted values,
original exceptions, disabled behavior and restoration. The recording logger
discards EventId and structured state; formatted strings cannot certify those
contracts. A logger that still logs after disposal is not a disposal spy.
`MessageActivityTests` establishes sampling, start identity, parent/kind/status,
remote-over-ambient priority, carriers and link/new modes. Same in-memory headers
are not durable-provider propagation. Hostile callbacks lack a preexisting ambient
control; destination tags and malformed carriers are not checked here.

`StartedActivityTests` checks status, exception text/tags, fake timestamp and body
length. Stable Duration after double Stop does not prove wrapper dispatch once,
because Activity.Stop is itself idempotent. Null/large body and some precise
parameter names are unarranged. These are not claims that all activity tests lack
callback counts: instrumentation below explicitly observes them.

### Monitoring — seven files / 2,472 lines

`BusHealthCheckTests` checks five health states, floor selection, exact error and
description, case-distinct endpoint URIs, cancellation with zero probe calls and
DI options/tags/factory guards. One Data.Value.ToString substring cannot establish
the exact keyed typed projection; typed multi-bus isolation is not exercised.
`MessagePipelineActivityTests` returns mutable observations after bus shutdown:
its terminal hostile/unsampled delivery count is genuine positive evidence. It
also proves real send/receive/process chain, baggage, trace-state and remote
context. The conditional throwing logger has no attempt count; stop-failure
header NotEqual accepts null. Destination/body tags are coarse. Listener disposal
before stop and lack of explicit ambient restoration leave separate gaps.

`MetricObservationSession` selects exact meter identity and captures descriptors,
tags and unique lookup keys. Its count waiter restarts timeout on each wake, so
unrelated events can remove the operation-wide deadline. Long-to-double storage
can lose precision above 2^53. In-flight disposal/constructor ownership requires
contract evidence rather than an assumed production defect.
`PayloadAdmissionTelemetryTests` has once-per-buffer rejection, independent
envelopes, exact fixed tag allowlist and an actually attempted hostile exporter.
It does not assert metric numeric values or per-operation exporter pairing;
canonical error type and a real payload sentinel are unarranged.

`MessagePipelineMetricsTests` checks exact instrument types/units/buckets,
consumer/handler classification, real failures/retry attempts, fake elapsed time,
DI lifetimes, outbox outcome distinctions and original first failure. Its gated
process has actual entry/release and [1,-1] evidence: retain that strong control.
Other active observations are globally sorted, not paired per operation. Equal DI
and context clocks cannot prove precedence. Provider A-to-B contamination is
forbidden, but after B traffic both nonempty collections do not forbid B-to-A.
Failed-factory disposal checks only Sent; multi-bus total two is not owner pairing.
Some snapshots precede stop. Unlike the failing factory, the throwing listener
does not count attempted callbacks. Created warm-up buses are discarded; startup
and serial cleanup/restoration are not uniformly failure-safe. Outbox metric
numeric values and temporal order are not asserted.

`ServiceBusInstrumentationTests` checks 17 instruments, exact distinct gauges,
future-age clamping, warning bits, context IDs, attempt changes and no raw-memory
tags. It explicitly counts one sample and one stop after failure/double disposal,
and counts broken-factory creation. Canonical enum assertions compare a sorted
set, not argument-to-label pairs. Histogram inputs are not matched to measurements;
descriptions are nonempty only. A disposed Activity start lacks a sampling
listener control; a disposed Meter cannot itself record a forbidden measurement.

`ServiceBusTelemetryTests` supplies approved source/metric/attribute ID sets,
uniqueness, old-export absence and exact remaining public surface signatures.
Normalization expectations often reference the same SUT canonical constants,
not independent literal values. InMemory has a literal positive control elsewhere;
this file alone cannot justify a graph-wide transport claim.

### Observers — four files / 884 lines

`MessageObserverTests` observes actual known messages, same markers, original
OnNext failure routed to OnError and an independent live handler. Counts and
negative fault-task snapshots precede shutdown; runner-cancelled cleanup is not
an independent failure-safe bound. Its intentional test observer OnCompleted is
not a productive dummy merely because it is a no-op.
`PublishObserverTests` establishes same pre/post message/context, response-only
send shape, repeated disconnection and same serialization exception. Request
callback assertions are type/stage rather than request identity; disconnected
publication has no consumed control. Handles are disposed before final shutdown.

`ReceiveObserverTests` establishes per-context order, known consumer/handler
correlations, same original ConsumeFault and actual post-next ReceiveFault,
without asserting a false global order. Completed consumer tasks do not prove
waiting for an actually held consumer. Success body and terminal callback counts
are less exact; early handle removal can hide late events.
`SendObserverTests` proves same bus/endpoint context, message and destination,
double endpoint disconnection plus actual later consumer delivery while bus
observation continues. Inverse connection order and explicit endpoint type/body
pairing are absent; early snapshots/removal retain the separate terminal gap.
The comment explaining concurrent request/response frame partitioning is current
functional documentation and should not be deleted as construction history.

### Operations, partitioning and runtime — six files / 1,079 lines

`BusProbeTests` rejects snapshot mutation and observes dynamic endpoint addition
and removal with distinct addresses and retained input. Three addresses are
counted but only two independently asserted; child ordering and post-build live
context detachment are unarranged. Dynamic-stop failure can skip harness cleanup.
`HealthReportExtensionsTests` parses actual JSON including typed numbers/bools,
indentation and omitted duration. Root and sole entry share Degraded, so wrong
aggregation from that entry survives; exact schema/multiple entries are absent.
`ProbeContractTests` checks precise guards, cancelled-token forwarding, fields,
null removal, collision retention, independent start/duration and probe identity.
Multi-entry failed Set does not inspect partial state; host projection is nonnull
only and live-context snapshot detachment is unarranged.

`Murmur3PartitionHashGeneratorTests` uses six independent literal vectors plus
empty/subspan/tail cases: strong bounded hash evidence. `PartitionedTaskExecutorTests`
has real held overlap, same-partition order, capacity overflow while independent
work executes, custom hash inputs and draining disposal. Max-two assertions occur
before releasing and draining all work, not at the terminal maximum. Immediate
second-task sampling is not a second-attempt barrier. Some waits/submissions
precede protected release; concurrent disposal lacks unconditional release.

`ServiceBusRuntimeLifecycleTests` observes startup cancellation, late/never Ready,
fake timeout/cleanup periods, original startup failure and real known messages,
publish bodies and pipe counts. The drivers require remaining-owner reading.
Equal default options cannot distinguish forwarding. Explicit type equals runtime
type; nonempty IDs are not paired to expected per-call IDs. Counts are not checked
again after rejected pre-cancelled calls.

### Topology — thirteen files / 2,269 lines

`ApplicationMessageTopologyTests` checks precise freeze errors, hidden exports,
capability registration and an actual nullable correlation. Four created bus
controls are discarded. An already-frozen global cache can hide which operation
caused transition; one capability value and no absent-nullable control leave gaps.
`CorrelationIdConventionTests` observes distinct known built-in/explicit IDs,
override and Empty policy, nullable presence/absence and class/interface presence.
Bootstrap remains a full-owner dependency; absent class/interface values and
terminal duplicate delivery are not established by first signals.

`EndpointConventionExtensionsContractTests` rejects 13 null cases and performs ten
real overload deliveries with paired IDs/kinds/headers. Its title does not prove
every missing argument for every current overload: no complete reflected API
matrix is reconciled. Per-ID first signals hide duplicates; explicit/runtime type
share an inherited route rather than competing routes. `EndpointConventionIntegrationTests`
supplies five distinct actual routes, a concrete override with terminal zero input
handler control, two real bus identities and dynamic factory laziness/freeze.
After fixed-route conflict, Send completion does not prove retained correct target;
first signals do not establish two-bus terminal isolation. Missing-route bus cleanup
and rejected-registration retained/no-ghost state remain open.

`TopologyConventionExtensionTests` checks precise receiver/value/delegate guards,
known required/optional-absent correlation and exact filter types. Partition/routing
formatters never execute; replacing a serializer checks type, not new content type.
Optional presence and attached child-builder state are absent. `TopologyConventionIntegrationTests`
has real known correlations and literal JSON media/body. Its interface/global
fixture names instead describe sealed records/per-bus selectors. Adding a raw
serializer with the same JSON selection does not distinguish selector effect;
it does not close the vendor-content-type update gap.

`ConsumeTopologyTests` checks existing/future projection identity and single/multi
composition semantics. Duplicate root projection can be replaced by an equal
trace; set-based ForEach hides duplicate calls, multi-All has only a false case,
and capturing builders drop filters. `MessageConsumeTopologyTests` supplies exact
delegation order/flags, same rejected/updated/replaced conventions and forbidden
callbacks, but trace-only bodies/dropped filters do not prove actual attachment.
`MessageCorrelationIdTests` checks required/nullable/null/Empty extraction and
unsupported property behavior, complemented by construction guards elsewhere.

`NamedEntityCollectionTests` is strong retained-state evidence: duplicate canonical
identity, two conflicts, subsequent free-ID registration, then terminal generic
and nongeneric ordered original/new entries. Do not claim all catalog tests lack
this proof. Throwing/reentrant comparer or concurrency policy needs source contract
reading before proposing extra behavior. `SendTopologyConventionTests` proves
known correlation, cache identity, compatible/universal/incompatible projections
and precise builder/value guards. Partition/routing/serializer filters are only
type-checked, not executed; distinct message slots and universal Apply are absent.
Correlation trait names alone do not prove a redundant legacy API.

`TopologyBoundaryTests` checks precise construction guards, limited cache surface,
same observer/resolver and 32 same-type cache results with one factory, plus a
different-contract second factory. Task.Run lacks forced contention or a bound;
the observer records last event, not event count. Adjacent NamedEntity tests close
their own retained-state path, not every different collection boundary here.
`TopologyEntityNameTests` checks constants, one suffix regex and bad length
parameters. One regex cannot prove claimed collision resistance; a constant
suffix can pass. Source entropy contract and exact valid boundary must be read
before a repair, rather than inferring defects from the test's name.

### Utilities — seven files / 1,413 lines

`AssemblyTypeCacheTests` has 32 same snapshots, Clear produces a distinct snapshot,
and an actual included type is found. A global Clear race requires relevant owner
usage evidence, not an assumed flake; scan count/contention/full post-clear catalog
are unobserved. `CancellationTokenExtensionsTests` proves initially pending signals,
successful active/pre-cancellation, linked target cancellation, None behavior and
precise missing-target names. Disposed-registration late cancellation is absent;
disposing a CTS is not cancellation.

`MultipleConnectHandleTests` has actual held async disposal, sync child invocation,
pending composite until release and same first failure while the second disconnect
still runs. Early failure has no unconditional held release/owned disposal, with
raw awaits and a local five-second bound. Repeated composite disposal and async
failure/inverse order are not established by this bounded matrix.

`TaskBlockingTests` proves pending background work, original failure identity,
exact cancellation token, noncapturing completion and an actually posted/pumped
capturing context. The after-entry cancellation thread lacks unconditional
cancel/release/join: CTS disposal does not terminate its wait. Pump cleanup can
skip after runner cancellation. Restored final context does not establish that
the factory never observes a temporary replacement. A synchronous mutant ignoring
cancellation requires a process guard, not an unbounded in-process claim.
`TaskCompletionSourcesTests` verifies exact creation option combinations and
idempotent successful completion. An unbounded completion await can hang when
completion is removed; prior fault/cancelled completion disposition is absent.

`TaskExecutorTests` has actual held Action/Task/ValueTask work, result values,
capacity admission/drain, cancellation versus accepted enqueue distinction,
disposal drain/concurrency, null guards and closed executor behavior. Its secondary
logger is actually attempted before throwing, follow-up work completes, and nested
finally restores ambient logging: a strong counterexample to coarse hostile-only
claims. Max-two is checked before final six-work drain. Blocking entry is before
the SUT call, not an admission-attempt barrier; some callback execution is not
observed. Many initial waits precede protected release, concurrent disposal lacks
unconditional release, and failed enqueue can skip disposal. Result tasks are
already completed; failure assertions use type/message rather than original
instance. Its blocking cleanup explicitly cancels/releases, unlike TaskBlocking.

`TaskResultsTests` proves exact successful/default values, cached task identity,
original fault and actual cancelled status, plus bounded core desktop-reference
absence. Cached cancellation uses its own cancelled token; absence of a supplied
pre-cancelled token is not a defect without reading the factory contract. Missing
completion awaits lack local guards. Core references are not the whole dependency
graph; Async naming/token necessity remains a separate productive-source axis.

## Consolidated deferred findings

This packet has 0 Critical / 9 High / 3 Medium / 2 Low grouped review findings,
all OPEN. Severity concerns proof effectiveness or test reliability, not an
unexecuted claim of a confirmed high-severity productive defect. Each group is
qualified by the positive evidence above. Groups overlap earlier open findings;
their counts must not be added as a deduplicated whole-product total.

| ID | Severity | Gap and deferred acceptance boundary |
| --- | --- | --- |
| CR129-H01 | High | Causal arrival and terminal observation: held callback/consumer completion, final duplicate/cross-delivery counts, actual later negative-attempt barriers and actual secondary-failure attempts are missing on the identified observer/multi-bus/partition paths. Preserve terminal activity observations, gated metrics and logger-attempt controls that already exist. |
| CR129-H02 | High | Exact ownership pairing: multi-bus callback swaps, scope slot/disposal, clock precedence, bidirectional provider contamination and per-operation active deltas are not fully distinguished. Require independent owner-to-result observations on the stated paths, not only unique IDs or aggregate counts. |
| CR129-H03 | High | Exact scalar/field projection: host/probe address, diagnostic text fields, JSON root aggregation, health keyed data and metric numerical values have coarse or equal-input checks. Distinguish each supplied field/value and its actual result; preserve existing literal gauges/hash vectors/fault scalars. |
| CR129-H04 | High | Execute configured topology behavior: formatter/serializer/filter type and identity checks do not establish actual routing key, partition value, replaced content type or attached filters. Remaining production/owner contracts determine intended behavior before changes. |
| CR129-H05 | High | Rejection atomicity/retention: several DI/route/multi-entry boundaries stop at exceptions or Send completion. Observe original retained maps/routes and absence of partial/ghost registration. NamedEntity retained ordered entries and open-consumer empty services already supply narrow strong controls. |
| CR129-H06 | High | Collectible owner lifetime: keep the relevant live cache/inspector contract distinguishable from collection of its owner when evaluating Type retention. Generated IsCollectible metadata alone is not unload acceptance. No actual failing GC run or productive leak is asserted here. |
| CR129-H07 | High | Failure-safe operation bounds and owned cleanup: restartable metric timeouts, pre-try waits/startup, held-release omissions, serial cleanup skips, raw waits and thread join/cancel gaps can hang or contaminate later checks. Preserve strong finally release/None cleanup paths and use actual contract/process bounds before mutation. |
| CR129-H08 | High | Real pending/typed forwarding: completed request proxies, disposed providers, equal explicit/runtime types and equal default options leave pending lifetime/overload/type/option forwarding insufficiently distinguished. Complete owning graph and source contracts before new test design. |
| CR129-H09 | High | Owned global transition: discarded warm-up buses and already-frozen topology/global cache can confound the operation alleged to freeze or clear state. Establish actual owner lifetime/initial state/transition and concurrency contract, without assuming every global test currently races. |
| CR129-M01 | Medium | Variant/boundary controls: culture is unchanged, some endpoint flags have one bit value, several nullable/empty/order/metric boundaries and fault partial-enumeration branches are absent. These are scoped gaps, not proof that adjacent valid controls are ineffective. |
| CR129-M02 | Medium | Independent canonical/schema expectations: enum-label sets can hide swapped pairs, SUT constants can share wrong canonical values, formatted logs discard structured state/EventId, and descriptors often assert nonempty only. Exact external contract/schema and argument-to-output pairs remain required. |
| CR129-M03 | Medium | API/fixture integration completeness: overload null matrices lack full current-surface reconciliation; scan/cache/event observations lack some exact catalog/count distinctions; completion fault/cancel policies and original executor exception identity remain unproved. Whole-owner parser/effective graph is still pending. |
| CR129-L01 | Low | Misleading fixture/test naming: interface/global labels describe per-bus sealed-record setup; length/regex does not justify collision-resistance claims. Rename only after actual productive contracts and call sites are understood, without feature loss. |
| CR129-L02 | Low | Fixture declaration/packing consistency: some nested helper accessibility/packed declarations differ from the intended type/file style. Defer manual corrections until the full owner is admitted; a test fixture no-op or capture helper is not a productive dummy by default. |

No actual mutation kill is recorded. Counterexamples stated here describe what
current assertions fail to distinguish, not mutants created or accepted. Any later
mutation result must have its own actual run, source binding, guard, terminal code
and independently checked kill/survivor classification.

## Historical validation, not a fresh iteration-129 run

Iteration 128 repaired constructor-owned ResourceCache state on linking/timer
initialization failure after complete relevant productive-path reading. API and
successful behavior are preserved. CS01's targeted fault regression, effective
cleanup mutants and portable interval contract remain OPEN; historical passes
do not complete that proof. Source SHA256 is
`1d6a4dd32d80e54a41d865a6b735f6729e0a7234e185f7d9aee6eefe798a95fb`.

Historical strict Core Release build actually exited 0, zero warnings/errors,
62.65s. Historical native MTP execution actually exited 0, 4,007 passed,
failed/skipped/pending/other 0. Build receipt SHA256:
`ecfe587bd95768693f55d8836c329fa4d3a9a45818b5e88fe162152c00dddcfa`.
Native receipt SHA256:
`e031cba468d2d928f9a8879f2aec73b92e4124bdad0a3adba959b533105b5dc1`.
Historical CTRF SHA256:
`c77926de8943f1c5b430f794ddf470fc1b5935663e09e518fb74b973e3ef3695`.
All 398 mapped cases in this new packet have historical passed records. Zero
additional cases were executed by iteration 129. Avoid an identical fresh rerun
for a source/test-unchanged reading-only checkpoint.

Prior architecture/Async receipts remain historical. Core coverage 81.0997% line /
73.3110% branch is neither a fresh measurement nor entire-product coverage.
Nullability 1,219 mutation targets/nine historical runs and the 18,824/20,045
package target mismatch do not constitute global mutation closure. Genuine durable
sender/cloud-provider acceptance, whole-product coverage/CRAP, bidirectional Async,
all source manual comments, naming/layout/dummy/directive/legacy and greenfield
architecture obligations remain active.

## Full personal-read manifest

Paths below are relative to `tests/ViciOne.ServiceBus.Tests/`. Each row binds the
complete file to input HEAD, its physical lines, declared methods and historical
case mapping. `MetricObservationSession` is a fixture with zero test declarations.
The diagnostic also rechecks all 255 prior files / 73,401 lines against the same
Git input. Method-name inventory is only a read-set diagnostic, never a language
parser or newly generated test design.

| Relative path | Lines | Methods | Historical cases | SHA256 |
| --- | ---: | ---: | ---: | --- |
| DependencyInjection/DependencyInjectionConfigurationContractTests.cs | 1339 | 34 | 35 | b80c817e90192db3a1729a6576749fceaecf76312b36c9de163be6b4eb40a95c |
| DependencyInjection/EndpointConfigurationTests.cs | 313 | 3 | 9 | 30ab0be2470928c304c5653d18e17b86933023368ce892fec0a7426df4a52e3b |
| DependencyInjection/EndpointRegistrationTests.cs | 52 | 2 | 2 | a1e2d6d70f70d67e9f1de10ee968f59c27b3df505542899ad0ca448e83bf0848 |
| DependencyInjection/GenericRequestClientTests.cs | 203 | 4 | 4 | 78a927963d91de863fe3730ae9d00cee40b21ff5e9474b311f29f2dffa038795 |
| DependencyInjection/HandlerRegistrationTests.cs | 397 | 3 | 18 | fc7f25bf3faa042502f06a5571fd3feb97fa43f684a21300ad1c37e65272e41e |
| DependencyInjection/MultiBusRequestTests.cs | 495 | 6 | 6 | d659fde936efd286a8181a868a3e8ebd7a75d08e84226ec8a6d6fba59c87dc26 |
| DependencyInjection/MultiBusScopeIsolationTests.cs | 180 | 2 | 3 | cac6ad7fa7da97c3ce6604c0def2060c71c461bdf7fbd847ef314b61f63f7f40 |
| DependencyInjection/ReceiveEndpointDependencyTests.cs | 111 | 1 | 1 | 3ad91a46b02a6fb0a08ef72e748ccffd0079a93626a1ab7caea1588e5cd50f16 |
| DependencyInjection/ScopedPipelineTests.cs | 232 | 2 | 2 | d41ccf623ea6fd6eb066e199357e0ed76fc5bcdbe8b21fdbdfdbec3e637f4005 |
| Diagnostics/MessageDiagnosticRedactorTests.cs | 286 | 10 | 10 | 117baaf9aec5c95b86cf8e81b1502c69a7a2974738a312f67aefbd81caf49b11 |
| Events/BusReadyEventTests.cs | 39 | 2 | 2 | a0a1dee4a38e54ca60be3dc19d660becc97ae27ab61e90badf82678c8391fd2c |
| Events/FaultExceptionInfoTests.cs | 749 | 29 | 29 | 3ca3279c645e2954ab33c5788e937e634211b926230b62074a83d6a26436f047 |
| Events/HostReadyEventTests.cs | 82 | 2 | 2 | 95857e67ff6c84cdfec93339bf1c048f261f9f3314f641460e226a9b48b6daa9 |
| Events/PolymorphicFaultDispatchTests.cs | 176 | 1 | 5 | 0baab4e6ce24f860f41d9cb697effe305292efd9c60cd1fa112f8800de9d1a76 |
| Events/ReceiveLifecycleEventTests.cs | 155 | 5 | 7 | 69f90129104d44b13ce38a03352eb9a590c05a08b69c0f5e8d24cf6afef446e1 |
| Internals/Reflection/DynamicImplementationBuilderTests.cs | 306 | 8 | 20 | 130d4ff97f343aabb0c5a575ff0b8bd400aedada6dd6351e599223581cd16fc7 |
| Internals/Reflection/MessageImplementationCacheTests.cs | 43 | 2 | 2 | 871c82d628d8e26820300398c3107df85aa863c734d6ed9bfe894326a131f0dd |
| Internals/Reflection/PropertyCacheContractTests.cs | 145 | 6 | 8 | 1fb95cc6fd38928d257ea4d3b68b27f1fe63b73d3e9c7d01813ca1301208599e |
| Internals/Reflection/ReadWritePropertyTests.cs | 271 | 12 | 12 | be6daf41710b5c9cc99d2656c1dd1dffd23cef3ee5008bc94ebac466afa76de4 |
| Logging/LogContextTests.cs | 358 | 10 | 10 | 337fe1fad23c817bef74c33871a53a1f8f9c515be20a4a4234aa74dad986729d |
| Logging/MessageActivityTests.cs | 261 | 5 | 7 | 439bb61a18b0163709e001039a4a3822e454c66cc746be9b69e1410c638effff |
| Logging/StartedActivityTests.cs | 119 | 4 | 4 | 4a1ff6c8a21713fc7a2df04ee919cfdffe44a14abf2fc38822c42c607a0be1fe |
| Monitoring/BusHealthCheckTests.cs | 250 | 5 | 9 | a2ed61556ee53b758d9996cb89a1331fcc1ba43f4ab3cca61dacbfb6c561a97b |
| Monitoring/MessagePipelineActivityTests.cs | 311 | 4 | 5 | 6a65ac294cba1e8479f7043c182694ce2fbba9b83c8ea134ffaac0a54db7928f |
| Monitoring/MessagePipelineMetricsTests.cs | 1024 | 22 | 22 | eaa289dd00c0db8ac7cb1650b20cbfe7e1da70783a56e22ac828449d6f873dc2 |
| Monitoring/MetricObservationSession.cs | 102 | 0 | 0 | 41c159a7730682a1c30d57d6c7e1c81c430fba11d9a9fb70ff827df85c7fb8ee |
| Monitoring/PayloadAdmissionTelemetryTests.cs | 127 | 2 | 2 | b4feab55d510506f13fd0028fbca77f005bdf1103b162a0de38d3efa7e920bdf |
| Monitoring/ServiceBusInstrumentationTests.cs | 400 | 3 | 3 | 973ad9e3e69ef2a19041a4d260ce9c681ae6b2a7d907134a1893565009f38907 |
| Monitoring/ServiceBusTelemetryTests.cs | 258 | 4 | 4 | 556b6d7d2d21338a2d409d537d50695f5f69486f836c220246aedc937f408186 |
| Observers/MessageObserverTests.cs | 149 | 2 | 2 | c47833621616c194d56027c528f0c4e0a6be0081d549bd248c8ec0ffb95f5532 |
| Observers/PublishObserverTests.cs | 195 | 2 | 2 | 746129497074b58c067059b1e68e37340537aa697f97ebf0bb88ff2d8ba510fd |
| Observers/ReceiveObserverTests.cs | 281 | 3 | 3 | a4ef395e401c4d9ef97cd08ae8c092c7179891d6b1f8a75e851c386f62ffba7a |
| Observers/SendObserverTests.cs | 259 | 3 | 3 | aedb5263665d4a7e9c588d00ba1ccf8f9cab0d518e6701d5ff75863dc55b22b5 |
| Operations/BusProbeTests.cs | 145 | 3 | 3 | 15a894935cf3bdbb72c20347ae18357c5b98b551cfa1ae1b49dd71b1f2432f67 |
| Operations/HealthReportExtensionsTests.cs | 51 | 2 | 2 | dc1a87fa25487f7001ec3b808ef4cae85c2a5654b93a05ac9f9a407ac3f7d06c |
| Operations/ProbeContractTests.cs | 151 | 5 | 5 | 800c60fa90c64aa1902d02b708e40e1ec6ad7a2358dafb5c24e8fee857215a72 |
| Partitioning/Murmur3PartitionHashGeneratorTests.cs | 37 | 2 | 7 | 7399ea9d917548a48340ced37b0a22b182848f71160769803ff4a65613389d94 |
| Partitioning/PartitionedTaskExecutorTests.cs | 465 | 9 | 9 | a8dc3162772816675caea6c2ae7b40aa50f4955b7eefa568dad3ed789c8e10eb |
| Runtime/ServiceBusRuntimeLifecycleTests.cs | 230 | 7 | 7 | f813d760ca4ff952975dc179c1d39c5421daac4a159e0ec8e6eb34f12185c39a |
| Topology/Configuration/ApplicationMessageTopologyTests.cs | 85 | 4 | 4 | cd5069b21f6a8f1ffd8e4f512abc08ff71900dbfd34ba09746978f06283a73aa |
| Topology/Configuration/CorrelationIdConventionTests.cs | 268 | 8 | 8 | b28b7b9b3cf0d395a927274c0a4adcd83a9f76677b4fa7c531106fc0f2b2a83e |
| Topology/Configuration/EndpointConventionExtensionsContractTests.cs | 159 | 2 | 2 | 7788ff488f7b3d284d0f5009210f2710835d904968a108d3cca70f43e70eda51 |
| Topology/Configuration/EndpointConventionIntegrationTests.cs | 366 | 8 | 8 | a0587708e4842ce867728fa992ba665f6d2e43daf896db3939428940f39fbac6 |
| Topology/Configuration/TopologyConventionExtensionTests.cs | 230 | 4 | 4 | 0062d35635a60d58044d62b9894516121f44761712cecd26d582f44131e80291 |
| Topology/Configuration/TopologyConventionIntegrationTests.cs | 120 | 2 | 2 | 0111a9302fccb92986a1a1dadc84da82b149d3f04b2811488fff816be0d7f454 |
| Topology/ConsumeTopologyTests.cs | 140 | 3 | 3 | 203e5a5541951585b9e3adb4b7eba9d4d050601001c1f86a8f2d3a1f3dd1e73a |
| Topology/MessageConsumeTopologyTests.cs | 200 | 4 | 4 | 9b86410e4f04b7b9f3a4e875d72a8fbd3738f5d0532ba526c0461eeefe55bf88 |
| Topology/MessageCorrelationIdTests.cs | 63 | 2 | 2 | 74a5884766da4edb082f49c23cfb5801c5564df339b9643cdbb7ea2efbc735a6 |
| Topology/NamedEntityCollectionTests.cs | 54 | 1 | 1 | 043fe857d500c316ee01dc91033bfc0ead14c4e5bdeea3a41fe49adff25ce05f |
| Topology/SendTopologyConventionTests.cs | 217 | 6 | 6 | f995b9584b902691bd21c68d9250b20292252eea811f1007fa080ccd44e1edc0 |
| Topology/TopologyBoundaryTests.cs | 331 | 14 | 16 | 2891828af6187d8225d6f1b9822923f7039f43361aa9a8690592d1fda0cd4bfd |
| Topology/TopologyEntityNameTests.cs | 36 | 2 | 3 | 18dd7b0f06a1c2bdc474683ed51b132906678310fcf49f2df03e60208a23b047 |
| Util/AssemblyTypeCacheTests.cs | 45 | 2 | 2 | bf18abd6283e26f90277f1e53defa156fb4bf0a38a3887af7425c3ebc188132c |
| Util/CancellationTokenExtensionsTests.cs | 96 | 6 | 6 | ee9f1edcb6593bf4fecc461e3a1b8229e5d8f0475639297d332f0714c74a9b8d |
| Util/MultipleConnectHandleTests.cs | 93 | 3 | 3 | c31df0ca72f76e0bc9d02c748ee1023b34d47df99e6544890e8878139db45a60 |
| Util/TaskBlockingTests.cs | 334 | 8 | 8 | 2c38d3debeda01eb048bb0d9861eefe868c3e549541ef9ac0e7c2f458838a0cc |
| Util/TaskCompletionSourcesTests.cs | 37 | 2 | 2 | 2f4dde1137264562d7e5dcf2c20da7e1fe615de28ad7c52a5322fa48efd306b0 |
| Util/TaskExecutorTests.cs | 717 | 21 | 23 | fa86e8f8b658000e1756816443d2bfb481d747b99044486be5a829245848eda5 |
| Util/TaskResultsTests.cs | 91 | 5 | 5 | 83db21ed3eff5876c957101e7075397e8659959a462c40bcf3efa493cfdb0361 |

Totals: 59 files / 14,429 lines / 338 declarations / 398 historical cases.
The personal-read diagnostic actually exits 0; all worktree bytes match input Git
blobs, all prior read bytes match, and declaration names reconcile to historical
record metadata. Raw directory:
`/private/tmp/vsb-iteration129-core-contract-reading.mQjnUy`.
`personal-read-bindings.log` SHA256:
`1c2ef9cd5909920c4e87d7158b62f8ed6c98aa662e67fe6eff8f7d743e451887`.
`authority-and-input-bindings.log` SHA256:
`5b7b5b647d72907ebbe488124887e9606a36ae69ac28124a7fbc7d835f9c2692`.
Raw files are ephemeral, not a cloud/test-result archive; the committed manifest
and report preserve the relevant findings and exact bindings.

## Carry-forward and concrete continuation

Iteration 128's eleven grouped findings remain OPEN; this packet's positive cases
do not silently close other paths. Cache CS01 implementation is repaired but proof
remains open; CS02–CS04/CT01–CT03, iteration-126 forwarding gaps, iteration-125 MT7,
iteration-124 JR6, global nullability/package mismatch and prior provider/coverage
obligations retain their existing dispositions. Do not resurrect the disproved
expired-resource duplicate-add leak: reprepare releases that candidate before
duplicate commit failure.

Next: finish the remaining 243 exact owning Core inputs and complete effective
project/build/packages/fixtures/data/execution-CI, full language parser and
GitReadSet admission. Then address the connected deferred findings and the actual
CS01 fault/cleanup proof under that admitted owner, with guarded effective mutations
and fresh bound verification. Continue the whole productive-src manual reading,
comments, greenfield API/type/layout/architecture and all original quality axes.
Previously resolved informational directory/context questions are not a new work
item; preserve the documented next action rather than reopening them.

Before claiming security, perform strict manual-manifest/history-tail diagnostics,
an exact four-file normal commit, annotated checkpoint tag, approved normal atomic
branch/tag push and independent keyed branch/tag/peeled-ref verification. At report
authoring these terminal receipts are still pending; actual post-commit raw logs
and the handoff must state their real outcome. Never call this tag A+ completion,
never force-push and never stage another repository's work.
