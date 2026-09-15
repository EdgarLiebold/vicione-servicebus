# Connected Courier, Futures, Scheduling, Retry and Harness Reading Checkpoint

## Disposition

The original whole-product greenfield A+ goal remains active. This intermediate
iteration completes a connected 93-file personal reading/review packet, not the
whole Core owner, productive source review, API modernization or A+ acceptance.
No productive source, test, project, dependency or code comment is changed here.
Every observation and this report are manually authored after complete code reads;
no source, test, comment or report generator is used.

Two concrete high-severity test synchronization/reliability problems are identified:
an unbounded Thread.Join in a lock-regression test and a scheduler-dependent
two-Task.Yield completion assumption. Additional lifetime and assertion weaknesses
are separated from unconfirmed productive defects. Existing strong causal, exact
identity, rollback and provider-option controls are explicitly retained below.
No defect is closed and no executed mutation kill is claimed in this checkpoint.

## Input and authority

Repository: repositories/vicione-servicebus.
Branch: feature/servicebus-a-plus-api.
Remote: origin, git@github.com:EdgarLiebold/vicione-servicebus.git.
Previously secured input: 311a2bea237e77dad480217631a9cdeb5f0a061d.
Productive source tree: 400a506421aa680730469d7bfbb4b78e364164b7.
Core owning tree: e3be6b831183b3b65636c3b5e167c165696037d2.

The main has fully read the applicable entry instructions and reuses unchanged
complete reads of the seven bound authorities. The exact current Licensing-only
DECISIONS addition has already been personally read and reconstructed against
the prior bytes. It grants no new ServiceBus product authority. The selected,
hash-bound Development Slice remains the product work truth.

| Authority | SHA-256 |
| --- | --- |
| AI_WORKING_AGREEMENT.md | e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e |
| GLOSSARY.md | 7ce780b178a971e40b57ee7ffb3bec472becdff96ef946726e0143339793adf7 |
| DECISIONS.md | 72d5f1b02a4b81f3b70c3699b2878768eec78eafda253be90df567c1858ce285 |
| current/README.md | a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39 |
| work/CURRENT_ORDER.yaml | 49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d |
| FINDINGS.md | 9a913a7937a5d216edc3ce83940c215a8d47e81e5823ccab286aff0cf2eea397 |
| DEVELOPMENT_SLICE.json | 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199 |

The Slice is work/delivery/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/
DEVELOPMENT_SLICE.json beneath vicione-architecture. Current authority/input and
all three unmodified history files verify again before the checkpoint edits.
Protected review/, TestResults/ and workspace vicione-legacy/ are not inspected,
enumerated or changed. Unrelated repositories and user work are preserved.

## Actual reading and reconciliation

The main personally reads every file listed in the manifest through EOF, including
all helper types, fixtures, callbacks, assertions and comments. Only complete,
visible reads receive credit. A context-truncated request for routing-slip/state
extension ranges is not credited; the unseen ranges are subsequently read in
bounded calls before either complete file is credited.

| Packet | Full files | Physical lines |
| --- | ---: | ---: |
| Bootstrap and imports | 2 | 36 |
| Courier | 25 | 7,168 |
| Futures | 16 | 3,413 |
| Scheduling | 8 | 1,412 |
| RetryPolicies | 13 | 2,182 |
| Testing, including Diagnostics | 29 | 8,482 |
| New connected full reads | 93 | 22,693 |
| Previously bound full reads | 314 | 87,830 |
| Cumulative Core owner full reads | 407 / 557 | 110,523 |
| Remaining owner inputs | 150 | 32,604 |

All 93 new and 314 prior files match exact input Git bytes. All 523 declared test
method names in the new packet reconcile with 597 historical passed native cases.
The first 64 files separately reconcile to 329 declarations / 397 historical cases.
Counts are not coverage percentages or evidence that the whole product is A+.
Regex declaration/name reconciliation is not a complete C# language parser.

Agreement section 4.3 remains binding: complete owning project and effective
shared build/dependencies, fixtures/data/execution CI, full parser and GitReadSet
admission precede new Core test design/editing and whole-owner acceptance. This
checkpoint reviews existing tests only. Proposed proof criteria below are deferred
contract questions, not prematurely designed new tests.

## Positive controls retained

### Courier execution and failure ownership

CourierActivityFactoryContractTests checks exact created/borrowed arguments/logs,
pipeline order, original failure identity, cleanup and ordered primary/cleanup
aggregate failures; pre-cancellation prevents creation/provider/pipeline work.
ScopeProvider tests resolve real DI dependencies and distinguish created disposal
from borrowed ownership. Completed fake async disposal proves invocation preference,
not independently held asynchronous cleanup completion.

RoutingSlipFaultIntegrationTests and RoutingSlipRetryIntegrationTests actually run
reverse compensation, different first/second log values, retry/redelivery attempts,
known tracking identities and terminal events. Always-failing compensation exhausts
the configured budget, so claiming that all compensation upper bounds are untested
would be incorrect. Several integration fixtures assert terminal collections after
shutdown; that positive control is not generalized to first-signal-only cases.

RoutingSlipBuilderContractTests rejects malformed variable sequences atomically,
exercises actual read-only mutation failures, keeps a second builder mutable, pins
fake builder time and exact constructor shape, and tests serializer/payload failure
causes. Payload integrations include independently different argument/variable
values, URI dispatch/compensation and an opaque private-state payload whose custom
converter is necessary to preserve nonzero values; this is not a tautological
round-trip or dummy fixture.

### Exact supplemental message data

RoutingSlipEventAccessorTests and RoutingSlipEventPublisherContractTests directly
require nonnull exact Variables across all nine event shapes, with actual data/
argument distinctions and supplemental send/publish selection. Publisher tests use
literal flags, excluded empty collections and poison payloads. These adjacent
controls qualify the conditional variable-dictionary assertion in the separate
CourierMessageContractTests: a global all-null Variables mutation would not evade
the whole already-read packet merely because that one local loop is conditional.

### Future state, terminal factories and replay

FutureStateTests actually mutates every assigned caller collection and checks
retained original pending/subscription/variable/result/fault contents. The
subscription equality test distinguishes each address and request-ID component.
State extensions preserve the independently different first fault timestamp,
retain the other pending ID until completion and retain lifecycle state when
null-producing factories fail.

FutureTerminalConfiguratorContractTests executes all event/state synchronous,
asynchronous and initializer result/fault shapes and retrieves exact stored values,
not just factory return values. FutureTerminalDispatchContractTests cancels inside
terminal creation, preserving the exact consume token with no result/send, and
checks result/fault rollback. The routing-slip default fault pins actual command,
tracking/message identities, fixed timestamp and removal of pending work.

BatchFutureIntegrationTests contains a held job-enter/release control, successful
and faulted command replay without extra attempts, and deferred subscriber behavior.
These are real same-process InMemory controls; they do not prove durable restart
or real cloud-provider acceptance.

### Scheduling options and adapter forwarding

ApplicationScheduleOptionsTests forwards independently different correlation,
conversation, message, request and scheduling-token IDs, exact payload/destination/
time/token, TTL, nonnull headers/null-header removal and partition key through the
actual provider send pipe and returned handle. Thirty-three adapter operations
check all exact arguments, relative destination insertion, once-per-call delegation
and lazy scheduler reuse. Explicit redelivery returns the exact initially incomplete
payload task and forwards exact delay, optional callback and independent token.

SchedulerTimeProviderTests fixes 2039 command time, typed/untyped delays and
scheduling metadata, exact resolver caller token, default/injected clock identity,
selected token ID and a UTC+03 local schedule. Both send/publish recurring controls
check ordered cancel/pause/resume commands with different nightly/operations IDs.
These positive operations qualify adjacent guard-only scheduling/provider files.

### Retry causality and acquired-context cleanup

Immediate and interval policies check exact complete budgets/counts/attempts/delays;
incremental intervals independently equal 2s, 5s and 8s. Exponential filter execution
exhausts the actual eleven-attempt budget and bounded fractional/cap scenarios.
DefaultTechnicalFailureClassifierTests distinguishes terminal/transient/custom/
provider/inner/aggregate evidence without message-text heuristics.

ConsumeContextRetryPolicyTests acquires actual wrapped contexts, injects admission/
projection/representation failure and ordered cleanup failure, disposes once and
then actually cancels the source; zero callback invocations establish removed
registrations on those paths. PipeRetryExtensionsTests holds pre-retry and terminal
callbacks behind entered/release signals, asserts incomplete operation, exact trace/
failure/token and uses finally release and bounded completion. This supplies real
await causality, not only Task.Yield or completed fake tasks.

### Harness traces, scope, retention and actual clocks

TrackedActivityTests checks one-tick idle/max boundaries, unrelated versus related
trace behavior, stale queued callbacks, disposal no rearm/listener retention and
constructor timer-creation failure restoring actual ambient activity. A child
created reentrantly by a root-start listener is tracked until it stops. Timers,
listeners and owned cancellation/drain helpers are read completely.

TelemetryActivityExtensionsTests combines real receive-completion and next timer-
change barriers with pending publish/send/request operations and exact tick idle
completion. ActiveScope tests suppress ExecutionContext flow for a deliberately
unrelated send and retain only known causal messages even when histories are off;
connection cleanup failures preserve original identities and dispose each handle.

Saga tests run typed event success/failure, exact lifecycle/state collections and
bounded histories with three different saga IDs. Query alpha/missing controls
actually distinguish matched and unmatched work. Scope scheduling checks real
initiating/filter GUID/header pairing. Send/publish observer tests independently
observe elapsed 1s/2s/3s/4s, beyond clock-reference identity. GatedConsumer does not
report consumed completion before actual work release. Hosted-service tests pin
start/stop/restart order, partial rollback, exact token sources and failed-stop retry.

## Scoped findings and deferred proof criteria

Grouped review: 0 Critical / 9 High / 3 Medium / 2 Low; all fourteen groups remain
OPEN. Groups overlap earlier findings and are not added as independent product
defect counts. H01/H02 identify concrete test problems. Other assertion weaknesses
do not establish a productive defect or an actually surviving/killed mutant.

### H01 — Unbounded synchronous lock-regression observation — OPEN

AsyncElementListTests.cs:211 calls producer.Join without a deadline inside the
snapshot filter. Under the lock-held regression being tested, the producer waits
for that lock while the filter waits for the producer; runner cancellation cannot
interrupt the synchronous join. The assertion can hang instead of providing a
bounded failure. After whole-owner admission, the correction must preserve actual
producer/filter overlap, fail in bounded time and drain owned work even on failure.
This is a test reliability defect, not a claim that the current source holds the lock.

### H02 — Scheduler-dependent ready-timeout completion assumption — OPEN

TestingServiceProviderExtensionsTests.cs:97–98 uses exactly two Task.Yield calls,
then requires connection.IsCompleted. Yields do not establish that the timeout and
cleanup continuations have completed. A correct provider path can still be pending.
The eventual correction must causally await the owned operation with independent
bounds and separately observe acquired endpoint cleanup, not rely on scheduler turns.

### H03 — Failure cleanup and temporal task lifetime — OPEN

TestingServiceCollectionExtensionsTests.cs:35–36 and :65 merely inspect
DisposeAsync().IsCompletedSuccessfully in void tests. That pins immediate disposal
and fails to own cleanup after earlier failure. DiagnosticOutputTests creates a
global listener outside await-using; an early assertion can bypass its disposal.
Several inactivity/timer tests do not own observer disposal or finally release/
cancellation; AsyncInactivityObserver is explicitly disposable in the read fixtures.
Many harness stops pass the runner token despite an outer None WaitAsync.
Eventual criteria require genuine owned bounded teardown, attempted remaining
cleanup after a failure and preservation of original plus cleanup error evidence.

### H04 — Distinguishable asynchronous completion and token source — OPEN

Completed fake scopes, factory Tasks and Task.Yield-backed result/variable factories
exercise invocation/values but do not independently hold completion. Future routing-
slip/planner successful forwarding uses explicit token equal to context token.
PendingFaultCollection holds an observer but does not assert its returned operation
is pending before release. Preserve the real redelivery task-identity, retry held-
callback and trace-boundary counterexamples; qualify only the remaining paths.

### H05 — Drained terminal multiplicity and negative events — OPEN

Several Future, explicit/scoped scheduling and harness assertions occur at the first
completion signal before shutdown, sometimes with Snapshot.Take or Select.Take.
They can hide later duplicates or forbidden notifications. Some Courier self-cancel
titles forbid domain faults without observing that forbidden terminal collection.
Wall-clock delay assertions measure elapsed time before all processing, not only
the configured delay. Actual after-stop Courier counterexamples remain positive.
Completion/drain ownership and independently causal clock evidence must be settled.

### H06 — Exact paired metadata, content and destination — OPEN

MultiTestConsumerBehaviorTests sorts two IDs and values independently, allowing
cross-association to pass. Concurrent logger entries check count/category, not each
distinct index. Cyclic timeline checks two same-type row names, not one per identity.
Scheduling cancellation pins time but not supplied token/destination; resolver
proxies ignore URI in selected paths. Some recorded IDs compare null projections;
other source/observer identities and known body values are genuinely exact.
Eventual criteria must discriminate each independently owned pair, not replace
legitimate serialization round-trips or interface fixtures with superficial checks.

### H07 — Failed work preserves unrelated and already-mutated state — OPEN

Future request/routing-slip rollback starts with no unrelated pending entries;
terminal rollback starts with one subscriber and empty prior results/faults.
Variable replacement failures start empty. The saga atomic-update failure at
DependencyInjectionTestHarnessTests.cs:276 immediately throws without first changing
the candidate; direct mutation followed by failure is therefore undistinguished.
Preserve existing exact wrapper/value/first-timestamp and strong invalid-variable
sequence retention controls. Source semantics and atomicity boundaries must be read
before proposing any behavior change or new regression design.

### H08 — Configured behavior and real provider ownership — OPEN

Registration descriptors, callback counts, configure delegates and probe metadata
do not automatically prove actual selected runtime behavior. Activity scope setter
fixtures discard supplied TransportContext; recurring/adapter results return null
or completed tasks. Scope partition selectors are invoked but real partition gating
is not held. Revision subscriptions target an input endpoint that already consumes
default-published events, so dropped explicit subscription can be masked. Registered
request destination clients are resolved but not dispatched; rider callback boolean
is not actual rider acceptance. Real options, endpoint pair and integration controls
are retained without extending them into unperformed durable/cloud tests.

### H09 — Whole surface and extra generic/policy arguments — OPEN

RetryFactoryTests exercises generic arities two/three but only checks first exception
type plus an unrelated type, not SecondFailure/ThirdFailure membership. Exponential
factory forwarding checks limit/filter rather than all min/max/delta fields; bounded
delay assertions can admit an always-maximum delay locally. EveryDiscovered type
and EveryConfigured direct consumer titles arrange one actual type. Those claims
require exact language/surface reconciliation and distinguishable extra-slot behavior.
Do not call this complete coverage or executed pseudo/real mutation acceptance.

### M01 — Boundary and collection variant symmetry — OPEN

Recurring cancel/resume guards sample schedule ID and pause samples group ID,
not both fields everywhere. Several null/negative guards omit parameter identity
or retained nonempty state. RowVersion setter/null/byte detachment is not in the
five FutureState collection scenarios. Result direct-return storage assertions are
shape-local; terminal configurator storage provides important adjacent controls.
Required variants must come from complete source contracts, not invented matrices.

### M02 — Canonical formats and taxonomy inventory — OPEN

URI canonical query expectations partly derive from the same serializer. Classifier
EveryOwned taxonomy still requires the actual productive exception inventory and
additional aggregate/inner contract reconciliation. Probe tests inspect bounded
policy metadata, not actual assembled consume behavior. Any .NET/API rule or timer
domain boundary used for remediation must be checked against primary documentation
and the actual productive path; context acceptance of TimeSpan.MaxValue alone does
not prove executable delay compatibility.

### M03 — Greenfield API semantic consistency — OPEN

Filter Any/None is not a simple inverse for excluded messages; this requires review
of productive naming/documentation, not mechanical replacement. Disposed timer-source
RestartTimerAsync succeeds where other owned-state APIs throw; actual ownership
semantics must determine whether that is intentional. Fluent *Awaited configurators
return binders synchronously and arrange later work, so an Async suffix must not be
imposed merely because their callbacks return Task. This is not the fresh complete
bidirectional Async/API review and supplies no all-src modernization certificate.

### L01 — Test names and functional comments overstate selected proof — OPEN

ReadOnly filter sets are nonreplaceable references to still-add-configurable sets,
not frozen collections. Some OwnedBounded/Every/Only/Atomic titles exceed arranged
evidence. The global collection comment says every listener test is serialized;
its declaration alone does not establish complete collection enrollment. All code
comments are read; required source changes remain manual after full understanding,
with no historical/process prose or inferred unsupported contract introduced.

### L02 — Formatting, helper naming and type/file layout — OPEN

TestingServiceProviderExtensionsTests.cs:180 packs StopAsync into one line. Several
implicit-private helper fields/methods and extra empty lines differ from neighboring
style. Nested fixtures are part of their test context, not automatically misplaced
productive types. Complete src/tests .NET type/path reconciliation and formatting
acceptance remain open; no formatting command or directive bypass is introduced.

## Verification receipts and limitations

Own raw directory: /private/tmp/vsb-iteration130-core-owner-reading.25GM5K.

| Receipt | Actual result | SHA-256 |
| --- | --- | --- |
| personal-read-bindings-first64.log | exit 0, 64 exact reads / 329 names / 397 historical cases | 2da62ff5b24888cc7e98abd3578e628141f78c54a7b89f0b32dca9e5943b8d17 |
| personal-read-bindings-connected93.log | exit 0, 93 exact reads / 523 names / 597 historical cases, prior 314 exact | 2d4a80cc8ab6e1aae4377fa063068bb611e75aa42ad29f92284845f87ce42d1e |
| authority-and-input-before-checkpoint.log | exit 0, seven authorities, input trees, old history bytes | a32e210f109da31cf51a53b0cbdc324102711611b5926a4a567974a87bb294fc |

Raw logs are local diagnostic artifacts, not portable new acceptance runs. Historical
iteration-128 native CTRF has 4,007/4,007 passed, zero other statuses and SHA-256
c77926de8943f1c5b430f794ddf470fc1b5935663e09e518fb74b973e3ef3695.
Source and Core owner trees are unchanged. No identical build/test rerun is billed
as progress for a reading-only checkpoint. No new coverage/CRAP, current global
Async/package inventory, target mutant or real provider result exists here.
Historical line/branch values are not represented as current whole-ServiceBus
coverage; no 100% assertion/API/parameter/branch correctness promise is made.

Earlier iteration-129 groups and Cache CS01/CS02/CS03/CS04/CT01/CT02/CT03, previous
test gaps, nullability/package mismatch and real durable-sender-provider acceptance
retain their actual dispositions. CS01 implementation repair is not its missing
fault regression/effective mutation/interval proof. No internal agent is started
here and no independent external Red Team acceptance is claimed; prior unadmitted
advisor attempts and scope violations remain preserved in their earlier histories.

Four owned report/history files are to receive a scoped normal commit, new annotated
checkpoint tag and approved atomic normal origin branch/tag push. Successful security
is reported only after independently keyed branch/tag-object/peeled-commit refs,
current HEAD and these four work/index paths are verified. No force push, broad
staging, source/test deletion or historical-tail replacement is authorized here.

## Complete new personal read manifest

All paths below are relative to tests/ViciOne.ServiceBus.Tests/. Every row means
FULL through EOF at the bound input; physical line counts are independently checked.
No incomplete range or another agent's read is included.

| Path | Lines | Read |
| --- | ---: | --- |
| Courier/ActivityDefinitionContractTests.cs | 87 | FULL |
| Courier/ContainerActivityRegistrationTests.cs | 94 | FULL |
| Courier/ContainerRoutingSlipOutboxRequestTests.cs | 149 | FULL |
| Courier/CourierActivityFactoryContractTests.cs | 700 | FULL |
| Courier/CourierActivityScopeProviderTests.cs | 427 | FULL |
| Courier/CourierConsumerKindContractTests.cs | 159 | FULL |
| Courier/CourierContextContractTests.cs | 144 | FULL |
| Courier/CourierHostResultContractTests.cs | 257 | FULL |
| Courier/CourierMessageContractTests.cs | 237 | FULL |
| Courier/CourierRegistrationBoundaryTests.cs | 248 | FULL |
| Courier/CourierTestSupport.cs | 221 | FULL |
| Courier/RoutingSlipArgumentIntegrationTests.cs | 125 | FULL |
| Courier/RoutingSlipBuilderContractTests.cs | 474 | FULL |
| Courier/RoutingSlipEventAccessorTests.cs | 202 | FULL |
| Courier/RoutingSlipEventPublisherContractTests.cs | 523 | FULL |
| Courier/RoutingSlipExecutorContractTests.cs | 208 | FULL |
| Courier/RoutingSlipFaultIntegrationTests.cs | 387 | FULL |
| Courier/RoutingSlipHostConfigurationTests.cs | 207 | FULL |
| Courier/RoutingSlipLifecycleIntegrationTests.cs | 180 | FULL |
| Courier/RoutingSlipPayloadIntegrationTests.cs | 399 | FULL |
| Courier/RoutingSlipRequestIntegrationTests.cs | 294 | FULL |
| Courier/RoutingSlipRequestProxyContractTests.cs | 325 | FULL |
| Courier/RoutingSlipRetryIntegrationTests.cs | 542 | FULL |
| Courier/RoutingSlipRevisionAndSubscriptionTests.cs | 372 | FULL |
| Courier/RoutingSlipSubscriptionCaptureEndpointTests.cs | 207 | FULL |
| Futures/BatchFutureIntegrationTests.cs | 347 | FULL |
| Futures/FutureBehaviorContextFactory.cs | 66 | FULL |
| Futures/FutureConfigurationContractTests.cs | 131 | FULL |
| Futures/FutureConsumerKindContractTests.cs | 285 | FULL |
| Futures/FutureLocationTests.cs | 77 | FULL |
| Futures/FutureMessageTests.cs | 122 | FULL |
| Futures/FutureRegistrationBoundaryTests.cs | 326 | FULL |
| Futures/FutureRequestConsumerIntegrationTests.cs | 153 | FULL |
| Futures/FutureRequestDispatchContractTests.cs | 175 | FULL |
| Futures/FutureRoutingSlipContractTests.cs | 395 | FULL |
| Futures/FutureStateExtensionContractTests.cs | 379 | FULL |
| Futures/FutureStateTests.cs | 78 | FULL |
| Futures/FutureSubscriptionTests.cs | 63 | FULL |
| Futures/FutureTerminalConfiguratorContractTests.cs | 302 | FULL |
| Futures/FutureTerminalDispatchContractTests.cs | 213 | FULL |
| Futures/FutureVariableContractTests.cs | 301 | FULL |
| GlobalUsings.cs | 5 | FULL |
| RetryPolicies/BaseRetryPolicyContextTests.cs | 119 | FULL |
| RetryPolicies/ConsumeContextRetryPolicyTests.cs | 393 | FULL |
| RetryPolicies/DefaultTechnicalFailureClassifierTests.cs | 114 | FULL |
| RetryPolicies/ExceptionFilterContractTests.cs | 71 | FULL |
| RetryPolicies/ExponentialRetryPolicyTests.cs | 119 | FULL |
| RetryPolicies/ImmediateRetryPolicyTests.cs | 38 | FULL |
| RetryPolicies/IncrementalRetryPolicyTests.cs | 60 | FULL |
| RetryPolicies/IntervalRetryPolicyTests.cs | 75 | FULL |
| RetryPolicies/NoRetryPolicyTests.cs | 40 | FULL |
| RetryPolicies/PendingFaultCollectionTests.cs | 234 | FULL |
| RetryPolicies/PipeRetryExtensionsTests.cs | 577 | FULL |
| RetryPolicies/RetryFactoryTests.cs | 144 | FULL |
| RetryPolicies/TechnicalRetryPolicyTests.cs | 198 | FULL |
| Scheduling/ApplicationScheduleOptionsTests.cs | 148 | FULL |
| Scheduling/ExplicitRedeliveryIntegrationTests.cs | 105 | FULL |
| Scheduling/RecurringSchedulerContractTests.cs | 221 | FULL |
| Scheduling/RedeliverExtensionsContractTests.cs | 93 | FULL |
| Scheduling/SchedulerProviderContractTests.cs | 59 | FULL |
| Scheduling/SchedulerTimeProviderTests.cs | 388 | FULL |
| Scheduling/SchedulingExtensionContractTests.cs | 252 | FULL |
| Scheduling/ScopedSchedulingTests.cs | 146 | FULL |
| TestAssemblyBootstrap.cs | 31 | FULL |
| Testing/ActivityTestHarnessTests.cs | 170 | FULL |
| Testing/AsyncElementListTests.cs | 246 | FULL |
| Testing/AsyncInactivityObserverTests.cs | 168 | FULL |
| Testing/BusTestHarnessLifecycleTests.cs | 98 | FULL |
| Testing/ConsumeObserverTests.cs | 223 | FULL |
| Testing/DependencyInjectionTestHarnessTests.cs | 878 | FULL |
| Testing/DiagnosticOutputTests.cs | 264 | FULL |
| Testing/Diagnostics/TrackedActivityTests.cs | 278 | FULL |
| Testing/DynamicReceiveEndpointConnectorTests.cs | 177 | FULL |
| Testing/HostedServiceLifecycleTests.cs | 360 | FULL |
| Testing/InMemoryTestHarnessBehaviorTests.cs | 805 | FULL |
| Testing/MediatorTestHarnessBehaviorTests.cs | 214 | FULL |
| Testing/MessageFilterTests.cs | 228 | FULL |
| Testing/MessageObservationListTests.cs | 502 | FULL |
| Testing/MultiTestConsumerBehaviorTests.cs | 158 | FULL |
| Testing/ObservableTimeProvider.cs | 245 | FULL |
| Testing/OpenTelemetryGlobalCollection.cs | 13 | FULL |
| Testing/QueuedCallbackTimeProvider.cs | 36 | FULL |
| Testing/RecordedMessageTests.cs | 123 | FULL |
| Testing/SagaPollingTests.cs | 476 | FULL |
| Testing/SagaTestHarnessBehaviorTests.cs | 782 | FULL |
| Testing/StateMachineObservationCollectorTests.cs | 73 | FULL |
| Testing/TelemetryActivityExtensionsTests.cs | 508 | FULL |
| Testing/TestHarnessObservationPolicyTests.cs | 362 | FULL |
| Testing/TestHarnessOptionsStartupValidationTests.cs | 85 | FULL |
| Testing/TestHarnessTimeProviderTests.cs | 404 | FULL |
| Testing/TestingServiceCollectionExtensionsTests.cs | 195 | FULL |
| Testing/TestingServiceProviderExtensionsTests.cs | 225 | FULL |
| Testing/TextWriterLoggerTests.cs | 186 | FULL |

## Concrete continuation

Continue the remaining 150 owner inputs: Initializers 46, Mediator 14, MessageData
13, Requirements/CoreRequirements.json 1, Serialization 48, Transformation 1 and
Transports 27. These total 32,604 physical lines. Finish effective graph/build/
packages/fixtures/data/execution bindings, full language parser and GitReadSet
admission before new Core test design or editing. Then address connected causal
regressions/mutations and actual source findings, including manually written current
functional comments after complete productive file reads. Iterate the full original
API/architecture/naming/layout/directive/dummy/legacy/feature/format/coverage/CRAP/
provider quality axes without feature loss; secure completed iterations normally.
