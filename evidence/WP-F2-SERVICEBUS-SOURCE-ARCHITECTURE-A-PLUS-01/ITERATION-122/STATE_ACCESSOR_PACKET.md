# State-accessor source documentation and owning-test read progress

## Authority, secured input and exact scope

The original unbounded whole-product A+ goal remains active. The ServiceBus
direct-Lead source-architecture slice remains SHA256
5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199.
The separate Licensing orders do not change its selected rules or write scope.
Protected review/result trees and unrelated user work are not read, changed
or staged. Source comments are handwritten with apply_patch after complete
personal understanding; no script generates code, comments or dispositions.

Input e280df694ab9e6c50e3877aebf97882dfeeacb5e is actually secured by
servicebus-a-plus-iteration-121-member-nullability-checkpoint-2026-09-15.
Annotated tag object 362d5fac27eb17eb4e845b9c723df62d258f8c98 peels to it.
Both the atomic normal push and independent reference-keyed remote query
terminate 0; branch, tag object and peeled tag are checked separately.
Raw input receipts are in
/private/tmp/vsb-iteration121-member-nullability.XygWpX.
Push SHA256: 39eea695d736a32153c6053c0cadcbbdc1ba29b071b6b8cf0244510ceb0102c7.
Remote-query SHA256: d5ed2ebe40c433dbdb62a9e41c9e81b04de646e584208b54bd3d82be1ea11d3d.

Six normative bindings remain unchanged. The DECISIONS binding changes from
07147cdb45bdd87950c3cd91705bd636fe7bcd08a13714ec7a874edad284627d to
43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4.
The main completely reads the new Licensing-only decision and surrounding
current entries. A read-only reconstruction excluding only the new decision
and its counter/index entries restores exactly the previous completely read
binding, terminal 0. Its unchanged remainder is reused, not falsely called a
new complete reread; no selected ServiceBus rule changes.

## Complete personal source reads and manual repairs

The main reads all eight related production files completely: 507 input lines,
517 final lines. Every source comment is checked after understanding the
corresponding complete file. DefaultInstanceStateAccessor, RawStateAccessor,
IStateAccessor and StateAccessorExtensions receive manual XML-only repairs.
IntStateAccessor and StringStateAccessor already have accurate summaries;
InitialIfNullStateAccessor and StateAccessorIndex have no stale comments.
The latter two neighbors were already personally read in iteration 120, so
this is not eight new uniquely read files in the whole-source census.

The corrected documentation describes actual current behavior:

| Source | Actual documented contract |
|---|---|
| DefaultInstanceStateAccessor | First read/write/predicate/probe selects exactly one public instance property typed IState, accepting non-public getters/setters; reads may initialize a null state through transition behavior |
| RawStateAccessor | Stores a state reference and resolves non-null reads through the owning machine by state name |
| IStateAccessor | Reads/writes current state and builds predicates for its stored representation; a read may initialize a missing state; nullable results are explicit |
| StateAccessorExtensions | Distinguishes direct-accessor and owning-machine forwarding, including nullable results and possible read side effects; accurately describes token forwarding |

No unconditional cancellation, rollback, atomicity or late-registration promise
is added. All bodies, signatures, names, parameters, directives and dependencies
remain byte-identical after removing only XML-comment lines. An exact eight-file
comparison terminates 0: four XML-only changes and four unchanged sources.
This bounded equivalence is not a certificate for unrelated earlier API changes.

Nested partial accessor filenames identify their containing state-machine type
and contained accessor. Their focused Accessors folder and the public Sagas
contract/extensions owner are coherent in this inspected scope; no physical
move is justified by this scope alone. Whole-product folder/type/API review
remains required. All source and reading hashes are in STATE_ACCESSOR_BINDINGS.md.

## Core owning-test admission: actual progress, not sampling acceptance

Core owner tree e3be6b831183b3b65636c3b5e167c165696037d2 contains 557 tracked
files, including 553 C# files. The main personally completely reads 27 owner
files / 7,104 lines: 24 C# files, the project, resolved lock and Protobuf test
input. Effective root/test build, central package and native runner policies
are read/reconciled separately. CORE_OWNER_READ_PROGRESS.md records every exact
completed path and SHA256. There are still 530 unread owner files in this bound
reading pass. No new Core test is designed, authored or changed, and no unseen
neighbor is accepted on the strength of this subset or a full test execution.

The existing tests provide concrete current cases for declaration styles,
state/event enumeration, raw/string/integer storage, inherited properties,
composite options/order, hooks, cancellation, compensation, retry, correlation
and nested request behavior. Examples already personally read include
Definition_InitializesAndEnumeratesTheExactStateEventAndReachableEventSurfaceAsync,
StateStorage_RoundTripsTheExactRepresentationAndPredicateTruthTableAsync,
CompositeEvent_DuplicateHandlingMatchesTheConfiguredOptionAsync and
Transition_CancellationDuringThePriorActivityPreservesTheCurrentStateAsync.
These are existing methods, not newly generated tests or whole-owner quality
certificates. Their actual conditions constrain the next source analysis.

One concrete local cleanup candidate is the duplicated consecutive NotNull
assertion in StateMachineLifecycleIntegrationTests.AssertInstance. Condition
and exception fixtures also use already-completed Tasks for several awaited
callback arrangements. These are observed properties of the read files, not
an exhaustive test-gap disposition; corrections and new owning tests wait for
complete admission rather than bypassing the current owner.

## Internal read-only counterreview

One separate internal Sol Lead advisory counterreview completely reads exactly
eight source files / 517 lines. Every supplied entry and exit SHA256 agrees.
It confirms lazy selection, exact IState property eligibility, initialization
and reread, forwarding, representation semantics and the bounded physical
owner. It finds no mandatory comment correction or stale/process narrative.
No product role, external independence or cloud acceptance is asserted; internal
reading never substitutes for the main's complete personal source reads.

The counterreview keeps runtime candidates separate: explicit call-token versus
context-token handling during initialization; writes preceding previous-state
lookup and observer completion; and the lazily frozen integer index under late
registration. They need full-path causal disposition and owning behavioral
proofs, not a documentation claim or a static candidate reported as a defect.

## Actual focused validation

SDK is actually 10.0.302. global.json selects Microsoft.Testing.Platform without
SDK pinning. Both existing test owners use their native MTP entry, not a new
runner or compatibility bridge. Known compiler/CLI sandbox IPC boundaries are
handled by targeted authorized escalation, not blind retries or policy changes.

Strict no-restore Release Core build terminates 0, zero warnings/errors (64.64s).
Strict no-restore Architecture build terminates 0, zero warnings/errors (23.66s).
These are focused incremental builds of the changed candidate, not a fresh
clean whole-product warning inventory. No full-workspace rebuild is claimed.

Fresh unfiltered native Core execution terminates 0: 4,007/4,007 passed,
zero failures/skips (20.439s). The actual compiled host's help first confirms
minimum-expected-tests, zero-tests-policy, fail-skips and report-xunit-ctrf.
Its registered no-progress option is marked deprecated; the subsequent
Architecture invocation uses the observed modern progress off option.
The exact four-source whitespace verifier terminates 0 with no output/writes.
Fresh unfiltered native Architecture execution terminates 0: 439/439 passed,
zero failures/skips (3m33.469s). Its independently parsed native report contains
exactly one passed bidirectional Async naming case (174206ms). Both reports
independently contain their exact expected passed-case counts and no non-passed
status. Persisted exact source and Core-read binding diagnostics terminate 0;
STATE_ACCESSOR_BINDINGS.md records actual receipt hashes. Raw artifacts belong to
/private/tmp/vsb-iteration122-state-accessor-read.CdDUDv.

Independent final table validation terminates 0: eight source input/final rows,
ten actual raw receipts and all 27 completed read rows / 7,104 lines agree with
real bytes. Receipt: evidence-bindings-verification.log, SHA256
0d00b54961707726e66caddaf6fc442df954f2b74718f5ce79c00fc01ebb880a.

No runtime mutation is relabeled as new for an executable/signature-equivalent
comment repair. No fresh package/cloud execution or global coverage result is
claimed. Iteration 121's real selected mutation kills and unchanged API-baseline
mismatch retain their original evidence and scope. A checkpoint is not a final
A+ release; its own commit/tag/push security follows actual terminal checks.

## Original-goal continuation

Finish the remaining Core-owner reading, then causally resolve the connected
NST/SMR declaration, cache, configuration, token, error-ordering and recovery
boundaries in their actual owning tests with independent red/green and effective
one-cause mutations. Do not replace the original goal with documentation closure.
Remaining entire-src personal reading/comment/type/folder cleanup, full API
modernization/metadata, custom retry extension equivalence, code/branch coverage,
CRAP risks, actual durable-provider acceptance and final multidimensional A+
review remain required. None is certified by these scoped source comments.
