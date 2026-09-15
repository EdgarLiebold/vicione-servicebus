# Recursive member-nullability API contracts

## Authority, secured input and scope

The original unbounded whole-product A+ goal remains active. The hash-bound
ServiceBus direct-Lead slice applies; the independent Licensing order does not.
The seven normative bindings remain unchanged. Protected review/result trees
are not traversed, read, changed or staged. Unrelated work remains untouched.

Input 197a150e35c542060e8d416a124fc93f49a3ebd9 is actually secured locally and
remotely with annotated tag
servicebus-a-plus-iteration-120-state-machine-source-checkpoint-2026-09-15.
Tag object 19d62bed2de7a00d102f6e83958fdf25887e2f49 peels to that commit.
The prior atomic normal push and independent reference-keyed remote verification
both terminate 0. Current HEAD, tracking branch and local peeled input tag agree.

Architecture test-owner admission reuses the completely personally read
c0ea82bf915b1f0c744cced8f80215bc6911f81b tree and personally authored/read deltas.
Current input owner tree is 69583711d5397f6dd8cf0aa5a0da2b52310c41f4. Its exact
diff from the admitted input has only the member-modifier test and catalogue
delta, both already personally understood. Existing effective policies and typed
inventory tests are read before authoring. Core execution is not full personal
Core test-owner admission and no new Core runtime test is authored here.

The sole tested source target is the actual PublicApiBaseline.cs. The main
completely reads all 449 input lines and the complete neighboring baseline,
generic and modifier tests. Tests, tooling and comments are manually written
with apply_patch; no generator or script writes source/tests/comments. The
current formatting repair changes no src signature, dependency or runtime body.
This tooling iteration adds no new uniquely personally read src file; it does
not inflate whole-source reading progress with internal-agent or test execution.

## Actual contract repair

The inventory now preserves recursive reference read/write states for
constructors, method parameters/returns, properties/indexers, fields and events.
Array element nodes and generic argument nodes remain in declaration order,
including unchanged sibling nodes whenever another node requires a payload.
Byref classification unwraps the CLR type without discarding its nullability.
Each member enumeration owns its NullabilityInfoContext reflection cache rather
than sharing a non-thread-safe cache across collectible package load contexts.

An omitted reference payload canonically means known NotNull in every relevant
position. Unknown is explicit, never renamed NotNull. Property root directions
are limited to externally visible accessors; absent/private directions use none
when a payload is needed. Child read/write positions remain independently
represented, including mutable elements returned by a read-only property.
Ordinary nullable values remain represented by their CLR Nullable<T> identity;
separately refined flow promises trigger payloads relative to the typed Nullable
default, rather than inventing reference contracts for ordinary value members.

Existing exact default/modifier/accessor/generic assertions remain intact. Only
intended newly visible nullability payloads are added to the corresponding old
expected records. No baseline is automatically regenerated or copied into place.

## Independent handwritten behavioral checklist

All methods below belong to
ViciOne.ServiceBus.Architecture.Tests.Tooling.PublicApiMemberNullabilityTests.

| Contract | Exact test method | Cases |
|---|---|---:|
| Required versus nullable parameter | ParameterNullability_DistinguishesNullableAndRequiredReferences | 1 |
| Required versus nullable return | ReturnNullability_DistinguishesNullableAndRequiredReferences | 1 |
| Nested generic positions | ParameterNullability_PreservesNestedGenericArgumentPositions | 2 |
| Array root versus element | ArrayNullability_PreservesRootAndElementNodes | 3 |
| Jagged and multidimensional nodes | ArrayNullability_PreservesJaggedAndMultidimensionalNodes | 2 |
| Property read/write promises | PropertyNullability_PreservesDistinctReadAndWritePromises | 3 |
| Field read/write promises | FieldNullability_PreservesDistinctReadAndWritePromises | 3 |
| Indexer value and parameter | IndexerNullability_PreservesValueAndIndexParameterContracts | 1 |
| Constructor parameter | ConstructorNullability_PreservesParameterContracts | 1 |
| Event delegate and payload | EventNullability_PreservesDelegateAndPayloadContracts | 1 |
| Ref/in/ref-readonly/out element | ByReferenceNullability_PreservesElementContracts | 4 |
| Generic use-site states | GenericParameterNullability_PreservesUseSiteContracts | 2 |
| Oblivious versus known reference | ReferenceNullability_DistinguishesObliviousAndKnownContracts | 1 |
| Hidden setter exclusion | PropertyNullability_ExcludesPrivateAccessorPromises | 1 |
| Read-only/write-only/hidden-reader/children | PropertyNullability_PreservesOnlyExternallyVisibleDirections | 4 |
| Parameter flow asymmetry | ParameterNullability_PreservesFlowReadAndWritePromises | 4 |
| Return flow asymmetry | ReturnNullability_PreservesFlowReadAndWritePromises | 2 |
| Nullable-value refinements and ordinary omission | NullableValueFlow_PreservesDirectionRefinementsWithoutInventingOrdinaryContracts | 1 |
| No invented value-only reference contract | ValueOnlyMembers_DoNotInventReferenceNullabilityContracts | 1 |

Nineteen methods represent 38 native cases and 19 exact requirement tuples.
The catalogue grows from 175 to 194 tuples. Exact projection execution, not this
handwritten table alone, proves correspondence. Typed CLR identity and independently
created raw nullability-state guards precede exact formatter assertions where
necessary; expected records do not call a duplicate formatter. Empty fixture
bodies are compiler-metadata arrangements, not production dummy implementations.
The narrowly scoped nullable-disabled fixture intentionally exposes oblivious
metadata. It adds no src directive and is not a convenience warning bypass.

## Observed red/green and arrangement boundaries

Initial strict build is terminal 0, zero warnings/errors (49.99s). Native red is
terminal 2: 27 cases, 25 failed, two passed, zero skipped (24.383s). Twenty-four
failures reach actual missing-output assertions. The remaining failure is an
incorrect NotNull assumption about the framework's bare constrained generic
method T state, not a product failure. Separating Required/Nullable fixtures
does not change that observed Unknown; arranged strict build is 0 and arranged
native red is 2, 25 failures (25.415s). The corrected independent oracle retains
Unknown, while generic declaration flags remain encoded separately. Exact
generic-use metadata binding remains an original-goal obligation.

First corrected strict build is 0. Full inventory/projection execution is 2:
112 cases, 106 passed, six older intended output deltas. All 27 new cases pass.
The old deltas are manually corrected and ten additional direction cases are
added. Expanded strict build is 0, zero warnings/errors (8.96s), and expanded
native inventory/projection is 0: 122/122 passed, zero failures/skips (31.655s).

The nullable-value flow candidate from internal advice receives an independent
handwritten test. Its strict red build is 0, zero warnings/errors (8.17s).
An initial bare-method filter selects zero cases and terminates 8; it proves no
behavior. The already proven class filter then terminates 2: 38 cases, one
functional missing-payload failure, 37 passed, zero skipped (28.438s). All four
independent raw state guards pass before that failure. The handwritten value-root
predicate is corrected. Its strict build is 0; native inventory/projection is
0, 123/123 passed, zero failures/skips (28.745s). Further mutation and final/
package/checkpoint outcomes are recorded only after actual observation.

## Assertion quality

All 63 physical assertion calls are personally inspected: 49 Equal, five Single,
three NotEqual, two Contains, one each DoesNotContain, NotNull, StartsWith and
True. Two Single calls are reusable helper assertions; 61 calls are in the 19
test bodies, averaging 3.21 physical calls per method, not runtime executions.
Every method asserts exact output or a meaningful negative inventory contract.
There are zero assertion-free, trivial-only or self-referential methods. Four
methods include negative assertions (21.1%). Equality/string/deep structure,
collection, runtime-type, Boolean and null-guard assertions serve actual formatter
contracts; exception/side-effect assertions are not added merely for diversity.

## Empirically executed mutation and final gates

All nine selected one-cause regressions strictly compile with zero warnings and
errors. Each complete 38-case native class run terminates 2 and detects the
injected change. M1–M4 use dotnet test; M5–M9 directly execute the identical
compiled MTP host, whose project explicitly enables the MTP runner. This avoids
repeated project evaluation, not discovery or assertion coverage. No runtime
speedup is claimed from the observed timings.

| Mutation | Failed cases |
|---|---:|
| M1 omit Unknown-root payloads | 5 |
| M2 remove generic-child detection | 3 |
| M3 remove array-element detection | 1 |
| M4 reverse generic-child positions | 2 |
| M5 ignore property writer visibility | 3 |
| M6 propagate root direction masks into generic children | 1 |
| M7 collapse write state into read state | 13 |
| M8 omit nullable-value root refinements | 1 |
| M9 remove event nullability emission | 1 |

Selected kills are 9/9, with 30 failed cases total; this is not exhaustive
mutation coverage or a whole-product score. Every candidate is immediately
manually restored to exact f62b2315... source bytes. M1/M2 restoration hashes
are observed in the tool trace; seven subsequent restoration logs persist the
same hash. An independent aggregate validator checks all nine strict logs,
native reports and seven persisted restorations and terminates 0.

Final strict Architecture build terminates 0, zero warnings/errors (5.97s).
Fresh unfiltered Architecture execution via dotnet test terminates 0:
439/439 passed, zero failures/skips (3m 29.337s overall CLI). The actual
EveryMethodName_MatchesItsAsynchronousContractBidirectionally case passes
(168,987ms). Both exact-scope whitespace verifiers terminate 0 without output
or source writes. Current entire src diff is empty; no new product API/runtime
change or feature removal is introduced by this repair.

Fresh package execution terminates 1 only at the unchanged baseline comparison.
Fail-fast stages before that comparison complete 31 packages, all 18 journeys,
three isolated testing consumers and 30 runtime assembly inventories. Fresh
output remains 20,045 lines, SHA256
4148abb6d0912c414a80048cd7e8b993ae66ba31c146564b135bd07d7ca69ce5.
Committed baseline remains unchanged, SHA256
59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b.
Independent duplicate-preserving comparison against the checksum-bound real
iteration-119 fresh output proves exact per-type CLR/default/modifier/generic
shape equality for all 3,230 blocks after removing only the new nullability
payloads. This does not certify the older 57 implementation-visibility changes
or custom retry extension equivalence. Current global line/branch coverage is
not measured here and remains required by the original whole-product goal.
All owned validation handles are terminal. The checkpoint is not a final A+
release; commit/tag/push security is credited only after its own terminal checks.

## Internal counterreview scope

The first bounded internal Sol counterreview completely reads four actual files
/ 1,209 lines, with matched entry/exit SHA256. It finds no mandatory reference-tree
correction and proposes the nullable-value flow boundary, which is pursued.
Its mutation candidates are static advice until actually compiled/executed.
The follow-up completely rereads Tool484/Test335 (819 lines) with matched
6d1913c0.../727e36c9... entry/exit hashes and verifies the two old tests unchanged.
It finds no mandatory executable correction. Its concrete comment correction
about nullable-value defaults is manually applied; final Tool SHA256 becomes
f62b2315d132da62ee743052d8d23c4c99f3fe787a22e12280117032de7e26ed.
This is not independent external/product/cloud acceptance or an exhaustive API
losslessness certificate. Internal reads never substitute for the main's reads.

Conditional flow relationships, exact generic-use/constraint annotation binding,
tuple/dynamic/function-pointer metadata and other previously open inventory
boundaries remain connected. NST-01–NST-10 and SMR-01–SMR-09 still require owning
behavioral disposition. Existing custom retry extension equivalence, provider
acceptance, remaining entire-src personal reading/comment/type/folder cleanup,
full code/branch coverage and final multidimensional A+ review are not proved
by these tooling cases. No whole-goal completion or global coverage is claimed.
