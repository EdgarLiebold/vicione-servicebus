# Plan — native test reconstruction

## Target

Rebuild every meaningful inherited test purpose as a source-owner test under `tests2`, using xUnit 4
on Microsoft Testing Platform 2. The permanent tree mirrors `src`; only `Architecture`, `Testing`,
and `Tools` are non-product owners. No phase name, generic `Core` bucket, Python test platform,
VSTest path, receipt, interceptor, or execution sentinel is part of the design.

## Current profile floors

- `UnitArchitecture`: 787 unfiltered cases;
- `LocalIntegration`: 3 unfiltered cases.

The floors are accepted lower bounds, not completeness evidence. Each executable project embeds one
durable product-requirement projection and compares it with passive metadata compiled into the same
assembly. The inherited semantic ledger remains the migration input until every disposition is
closed; it is never the runtime test architecture.

## Implementation order

1. Complete one coherent source-owner cohort from product source, inherited behavior evidence, and
   the semantic ledger.
2. Keep unit, local-integration, broker/database, and external-resource profiles separate.
3. Run locked restore, Release build, unfiltered MTP tests, bounded formatting, static test-quality
   review, and targeted false-green mutations. Only the Lead starts .NET/MSBuild processes.
4. Commit and push a stationary accepted cohort before beginning the next one.
5. Remove an inherited file only when all meaningful behavior it owns has an accepted replacement
   or an explicit non-product/non-executing disposition.
6. Remove the inherited runner and TestFramework only after complete closure, then atomically rename
   `tests2` to `tests` and update every solution, CI, documentation, and build path.

## Current work

The complete Abstractions, Analyzer, SignalR, MessagePack, and StateMachineVisualizer owners and the
Cron-expression scheduling cohort of the core owner are reconstructed. MessagePack
owns 49 native cases that replace 67 inherited module obligations and the six MessagePack-body
obligations previously assigned to the inherited mixed core fixture, including the
real in-memory pipeline and redelivery boundaries. It does not reuse the inherited TestFramework or
the old parameterized fixture hierarchy. Product behavior is unchanged; the only product-project
change is the signed friend grant needed to inspect the internal hardened option owner directly.
The accepted Git commit and tree, rather than a self-referential hash inside this file, are the review
identity. StateMachineVisualizer owns seven native behavior cases plus its projection case and maps
all nine inherited visualizer obligations individually. The Cron cohort maps all 58 inherited
obligations, uses deterministic UTC or test-owned time zones, and hardens repeated-whitespace
parsing with a minimal product correction.

## Accepted cohort — endpoint-name formatters

`Configuration/EndpointNaming/EndpointNameFormatterTests.cs` in the existing core-owner test project
contains nine ordinary xUnit methods that materialize 17 cases:

1. three snake-case word/digit/acronym variants;
2. nested namespace inclusion;
3. namespace plus prefix;
4. generic-consumer naming from its message type;
5. namespaced message naming;
6. four default/kebab/snake prefix-separator variants;
7. the prefix-free kebab message name;
8. the concrete consumer-definition plus kebab instance-id contract;
9. four exact `ConfigurationException` boundaries for suffix-only consumer, saga, execute activity,
   and compensate activity types.

The embedded core requirement projection has one row per method and the inherited disposition has
one terminal row per old case. Focused and full Release checks, bounded formatting/analyzers, static
assertion and gap review, and four targeted product mutations cover formatting, generic ownership,
instance-id sanitization, and suffix-only rejection. The predeclared UnitArchitecture floor is 635.
Only `tests/ViciOne.ServiceBus.Tests/EndpointName_Specs.cs` was removed after all 17 rows closed; no
product file changed.

## Accepted cohort — message URNs

Use the existing `ViciOne.ServiceBus.Abstractions.Tests` project and add two source-mirrored files:

1. `MessageUrnTests.cs` owns attributed/default/custom URNs, attributed arrays, plain/nested/closed
   generic names, consistent null/open-generic rejection, and four `Deconstruct` shapes;
2. `Attributes/MessageUrnAttributeTests.cs` owns null, empty, whitespace, duplicate-default-prefix,
   and invalid-custom-URI validation.

Thirteen ordinary xUnit methods materialize 17 cases. One passive requirement-projection row per
method and the terminal disposition map close the 12 old MessageUrn fixture obligations, the two
attribute obligations from `MessageType_Specs.cs`, and the existing Deconstruct gap. The validation
methods assert the public exception contract directly and deliberately do not preserve the
inherited static-cache wrapper. The UnitArchitecture floor is 652. Bounded format/analyzer checks,
assertion/gap review, and five one-cause mutations for derived names, array attribute propagation,
runtime-overload validation, constructor validation, and deconstruction pass. Both complete Release
profiles pass at 652/652 and 3/3. `MessageUrnSpecs.cs` was removed only after its 12 rows closed;
at that cohort boundary `MessageType_Specs.cs` remained untouched. C9 later replaced its independent
array-publication behavior and removed the now-fully-closed mixed fixture.

## Accepted cohort — request-rate algorithm

`Util/RequestRateAlgorithmTests.cs` belongs to the existing Abstractions test project. Six ordinary
xUnit methods materialize eight cases: generic flow completion, deterministic grouped/ordered
repetition, the exact request-growth curve, prefetch clamping, full/empty single-request boundaries,
and both zero-option guards. One passive projection row per method and a terminal disposition map
close the seven R0 obligations.

The concurrency test uses a task-completion barrier sized from the algorithm's public RequestCount;
it contains no `Task.Delay`, random input, polling, or environment dependency. Its deliberately high
result capacity isolates the named request-concurrency contract. The UnitArchitecture floor is 660.
Focused format/build/test, three consecutive stability runs, static assertion review, and six
one-cause mutations for callback processing, grouped request overlap, scaling, result clamping,
single-request stability, and constructor validation pass. Both complete Release profiles pass at
660/660 and 3/3. `PollingAlgorithm_Specs.cs` was removed only after all seven rows were terminally
mapped.

## Accepted cohort — reflection accessors and static-property metadata

The Abstractions owner adds eight ordinary xUnit cases under the source-mirrored
`Internals/Reflection` and `Internals/Extensions` paths. They preserve private-setter access through
the compiled accessor and cache, the five inherited static-property reflection boundaries, and the
standard .NET missing-key Try contract. The last item corrects an invalid R0 gap: `TryGetValue`
returns `false` and a null output for an absent name; it does not translate absence into an
exception. The unreachable catch in the product implementation is removed without changing its
observable behavior. Ten obligations close; the separate implemented-message-type fixture remains
open until its topology semantics can be proved rather than guessed. The UnitArchitecture floor is
raised to 668 before the first result.

## Accepted cleanup — inherited Abstractions project

All 78 obligations owned by the inherited Abstractions test project have terminal dispositions:
70 are replaced by native xUnit/MTP behavior and eight are non-product or non-executing. The old
formatter corpus is byte-identical to the embedded replacement. The ten executable NUnit source
files and their obsolete project are therefore removed.

The nine compile-only usage files were never tests. Their public consumer, saga, routing-slip
activity, definition, contract, topology, publish, response, header, and timestamp shapes now live
in the non-packable `samples/OrderWorkflow` project with source-oriented folders and namespaces.
Two repository architecture cases enforce non-packability and Engineering-solution membership,
raising the UnitArchitecture floor from 668 to 670.

## Accepted cohort — implemented-message topology

`Metadata/ImplementedMessageTypeCacheTests.cs` belongs to the Abstractions source owner. Six ordinary
xUnit cases replace the inherited count-only assertion with exact topology contracts: immediate
interface inheritance, diamonds without duplicate transitive edges, a class base plus an interface
inherited through that base, the corresponding polymorphic `Fault<T>` edges, exclusion of invalid
System interfaces, and the no-parent boundary.

The product implementation is made independent of reflection enumeration order. It retains the
interface edge inherited through a base class because an excluded base topology must not hide that
separate message contract. The UnitArchitecture floor is raised from 670 to 676 before the first
result. `ImplementedTypeCache_Specs.cs` is removed after its obligation is terminally mapped. The
focused and unfiltered profiles are green, and one-cause mutations prove interface reduction,
base-inherited interface preservation, polymorphic fault projection, invalid-interface filtering,
and the direct-edge marker.

## Accepted cohort — polymorphic faults

The mixed inherited `FaultPoly_Specs.cs` fixture is split by the product sources it exercises.
`Metadata/FaultMessageTypeTests.cs` in the Abstractions owner asserts the exact type and URN sets for
class- and interface-derived `Fault<T>` contracts. `Contexts/ConsumeContextEndpointExtensionsTests.cs`
in the core owner uses a real temporary in-memory receive endpoint to prove that a handler failure
for a derived interface publishes a fault consumable through the base interface, preserving the
message, supported message types, host, fault identity, and exception information.

Three ordinary xUnit cases replace five inherited obligations and raise the UnitArchitecture floor
from 676 to 679. The test uses the public endpoint contract directly; it neither inherits from nor
recreates the NUnit TestFramework fixture. The inherited mixed file is removed only after both
source-owner projects and the complete profile pass.

## Accepted cohort — array message publication

`Serialization/ArrayMessageTypeTests.cs` resolves the inherited fixture's contradictory name by
stating the actual supported contract: a public one-dimensional array is one message, not a batch.
The ordinary xUnit case exercises the real in-memory publish, System.Text.Json envelope, and consume
path and asserts the exact element count, order, and values. The UnitArchitecture floor rises from
679 to 680. The remaining two tests in the inherited mixed fixture were already replaced by the C3
MessageUrn source-owner cohort, so the inherited file is now fully removed.

## Accepted cohort — future locations

`Futures/FutureLocationTests.cs` mirrors its product source and replaces the inherited randomized
round trip with a fixed external identifier. It proves exact ID preservation and short queue-address
normalization, then closes the known missing-ID gap and hardens empty, malformed, duplicate, null,
relative-URI, and missing-endpoint inputs. The product now exposes stable public exception boundaries
instead of leaking parser, LINQ, or null-reference implementation exceptions. Nine ordinary xUnit
cases raise
the UnitArchitecture floor from 680 to 689; the fully replaced inherited fixture is removed.

## Accepted cohort — task executor

`Util/TaskExecutorTests.cs` mirrors the product utility and replaces the inherited delay-based,
assertion-free cases with deterministic execution contracts. Seventeen native cases cover action,
Task, and unambiguous ValueTask execution; synchronous and asynchronous results; constructor
boundaries; maximum concurrency; bounded backpressure; queued cancellation; unwrapped faults; Push
drain behavior; idempotent disposal; post-disposal rejection; and every public null-delegate
boundary. The product adds explicit ValueTask entry points, stable validation and disposal behavior,
and correct canceled-task completion. The UnitArchitecture floor rises from 689 to 706; the fully
replaced inherited fixture is removed only after all three ledger obligations become terminal.

## Accepted cohort — task utilities

`Util/TaskUtilTests.cs` mirrors the synchronous task utility and replaces the inherited NUnit
fixture with 18 ordinary xUnit cases. Explicit barriers prove completed and later results,
unwrapped generic and non-generic faults, caller-token cancellation, synchronization-context
preservation, and the exact externally pumped captured-context boundary without fixed-time behavior
assertions. The same cohort covers every remaining public utility capability: cached values,
canceled-task state and token, asynchronous completion-source options, cancellation registrations,
idempotent completion, invalid inputs, and the absence of desktop Windows framework references.

Seven one-cause product mutations and one missing-requirement mutation are rejected. A surviving
first version of the AggregateException mutation exposed an uncovered generic overload; the test
was strengthened and the same mutant then failed for the intended reason. The UnitArchitecture
floor rises from 706 to 724, and `AwaitSemantics_Specs.cs` is removed only after all nine inherited
ledger obligations are terminally mapped.

## Accepted cohort — task initializer projections

`Initializers/TaskInitializerExtensionsTests.cs` mirrors the product initializer source and replaces
the inherited NUnit fixture with 15 ordinary xUnit cases. The product API is normalized from eleven
duplicated `Select` overloads to explicit asynchronous `SelectAsync` and
`SelectOrFallbackAsync` contracts. Reference and nullable-value projections retain constant, lazy
synchronous, and awaited asynchronous fallbacks; string-only duplicates are removed without losing
a capability. Null source values, exact source/selector/fallback fault and cancellation state, input validation, null
fallback results, and every selected-versus-fallback branch are explicit.

Ten independent one-cause mutations are rejected: null-source selector invocation, both constant
fallback branch inversions, eager invocation of all four factory overloads, source-exception
wrapping, fallback-exception wrapping, and missing requirement metadata. The UnitArchitecture floor rises from 724 to 739, and
`TaskExtension_Specs.cs` is removed only after all ten inherited ledger obligations are terminally
mapped.

## Accepted cohort — header initializer convention

`Initializers/Conventions/DefaultInitializerConventionTests.cs` mirrors the owning product source.
One ordinary xUnit case replaces the inherited publish behavior through a real temporary in-memory
endpoint and observes the initialized `PublishContext` directly. It verifies ResponseAddress,
RequestId, exact five-second TimeToLive, typed custom header values, single-underscore-to-dash and
double-underscore-preserving name normalization, and delivered message content without a wall-clock
assertion. A second case gives the public convention a stable null-property boundary and preserves
the empty-header-name non-match.

Five one-cause mutations are rejected: custom-name normalization, standard-header prefix, null
validation, TTL-inspector omission, and missing requirement metadata. The UnitArchitecture floor
rises from 739 to 741, and `HeaderInitializer_Specs.cs` is removed only after its sole inherited
ledger obligation is terminally mapped.

## Accepted cohort — state property conversion

`Initializers/PropertyConverters/StatePropertyConverterTests.cs` mirrors the owning product source.
One ordinary xUnit case replaces the inherited state-machine fixture through a real temporary
in-memory endpoint. It proves that an integer-backed saga state is resolved after the transition,
awaited by the message initializer, converted to the exact state name, serialized, published, and
consumed together with its correlation ID and custom header. A fixed saga ID is behavioral input;
the central operation timeout is only a safety boundary.

Four effective one-cause mutations are rejected: wrong state-name conversion, skipped asynchronous
property conversion, changed custom-header name, and missing requirement metadata. The
UnitArchitecture floor rises from 741 to 742, and `State_Specs.cs` is removed only after its sole
inherited ledger obligation is terminally mapped.

## Accepted cohort — message initializer scalar conversion

`Initializers/MessageInitializerScalarConversionTests.cs` mirrors the root initializer pipeline.
Five ordinary xUnit cases replace the inherited conversion fixture with strongly typed,
deterministic inputs. They cover value-type-to-string conversion, exact scalar and object copying,
nullable-source unwrapping, invariant round-trip string conversion, and non-nullable-source
wrapping into nullable targets across bool, integer widths, double, decimal, DateTime,
DateTimeOffset, TimeSpan, enum, string, and object/URI values.

Six one-cause mutations are rejected: value-to-string conversion, exact property copying,
nullable-source unwrapping, string-to-int conversion, nullable-target wrapping, and missing
requirement metadata. The UnitArchitecture floor rises from 742 to 747, and
`Initializer_Specs.cs` is removed only after all five inherited ledger obligations are terminally
mapped.

## Accepted cohort — message initializer object graphs

`Initializers/MessageInitializerObjectGraphTests.cs` belongs to the existing core source-owner test
project. Six ordinary xUnit methods retain the six independent inherited obligations: covariant
fault projection, nested anonymous objects into private setters, interface targets from anonymous
and concrete inputs, and concrete targets from anonymous and same-type inputs. Every method has a
separate requirement projection row and explicit output assertions; the former assertion-free
read-only-property cases are not copied.

The cohort uses only deterministic test-owned contracts and host metadata. The fault test asserts
projected values instead of generated IDs, timestamps, or reconstructed reference identity. The
anonymous interface case asserts only supplied writable input; the concrete-source interface case
also asserts its exposed computed read-only value. Concrete targets prove the computed property
through the initialized writable property. Its six-row terminal disposition closes all inherited
rows. The UnitArchitecture floor rises from 747 to 753; focused and unfiltered gates plus four
one-cause mutations pass, and only the now fully replaced `Class_Specs.cs` is removed.

## Accepted cohort — dictionary and ExpandoObject initialization

Split the six inherited `Expando_Specs.cs` obligations by actual product owner instead of retaining
the old mixed fixture. Four dictionary/ExpandoObject behaviors belong in
`Initializers/Conventions/DictionaryInitializerConventionTests.cs`; converter availability belongs
in `Initializers/PropertyProviders/PropertyProviderFactoryTests.cs`; the exact concrete nested DTO
behavior extends `Initializers/MessageInitializerObjectGraphTests.cs`.

Each obligation receives one ordinary fact and one passive requirement-projection row. Fixed Guids
and timestamps replace random and wall-clock input. Assertions cover all supplied scalar/enum/Guid
values, exact concrete DTO preservation, converter presence, nested dictionary values, and every
exposed value of the single nested ExpandoObject order. Add a six-row terminal disposition, raise
the UnitArchitecture floor from 753 to 759, run focused and unfiltered gates plus one-cause
mutations, and remove only `Expando_Specs.cs` after complete closure. The five effective product
mutants respectively break enum conversion, dictionary lookup, nested-contract conversion, exact
property copying, and list conversion; the requirement-projection mutant independently rejects
unbound metadata. The first attempted direct-long enum mutant was correctly excluded because boxed
dictionary values use the object overload and the mutation did not affect the exercised path.

## Accepted cohort — message-initializer capabilities

Replace the 27 obligations from `MessageInitializer_Specs.cs` with 13 ordinary xUnit/MTP facts
split by actual product source owner. Retain three request/response behaviors on a real temporary
in-memory bus; move pure dictionary, array, list, scalar, object-graph, exception, variable, and task
behavior to their corresponding initializer subtrees. Do not reproduce the inherited one-time
fixture, console output, or its shared response object.

The result must preserve property merging and defaults, chained nullable conversion, partial
interface initialization, dictionary key/value/nested conversion, scalar and Uri boundaries,
array/list ordering, most-derived duplicate-property selection, nested interfaces, exception
projection, initializer-variable consistency, and nested-task completion. All inputs are fixed
except values whose generation is itself the contract; generated variables are asserted by captured
value or invariant relationship, never by the wall clock. Add a 27-row terminal disposition, raise
the UnitArchitecture floor from 759 to 772, run source-owner and unfiltered gates plus independent
one-cause mutations, and delete only the fully replaced inherited file.

The 27 inherited obligations are terminally mapped to 13 facts. Nine independent product mutations
reject missing request/response merging, empty dictionary/array/list output, incorrect Uri
conversion, base-property selection, missing exception projection, per-variable rather than
per-context identifiers, and incomplete nested-task awaiting. A tenth mutation proves that passive
requirement metadata cannot drift from its embedded projection. Two exploratory list mutations were
excluded: one hit a different already-covered converter path and one changed an unselected fallback.
The actual C19 path was then identified and killed, and the overstated materialization wording was
corrected before acceptance.

## Accepted cohort — property-provider factory matrix

Replace the 42 obligations from `PropertyProvider_Specs.cs` with 14 ordinary xUnit/MTP behavior
facts grouped by the actual `Initializers/PropertyProviders` branches: asynchronous source values,
task result values, arrays/enumerables, dictionaries, scalars/nullables/objects, enums, Uri values,
nested message contracts, exception information, and initializer variables. Add one separate
hardening fact for the public `TryGetPropertyProvider` false path.

Use the public factory and the returned real provider for every case. A small shared reader may
construct the `InitializeContext`, but it must not own discovery, results, coverage receipts,
retries, timing, or verdicts. Preserve exact outputs and all unusual boundaries from the inherited
matrix while consolidating only cases whose setup and semantic owner are genuinely the same. Add a
42-row terminal disposition, raise the UnitArchitecture floor from 772 to 787, run focused and
unfiltered gates plus independent one-cause mutations, and delete only the fully replaced inherited
file.

The 42 inherited obligations are terminally mapped to 14 behavior facts, and the factory false path
is covered by one independent hardening fact. Twelve effective product mutations reject missing
async conversion, task-result wrapping, converted-array output, dictionary-key conversion,
message-contract initialization, scalar/type conversion, object boxing, variable result conversion,
exception projection, Uri conversion, long-to-enum conversion, and false-positive provider
creation. One additional metadata mutation proves that passive requirement metadata cannot drift
from its embedded projection. An exploratory mutation of the exact array converter was excluded
because exact arrays correctly use the matching input-provider branch instead of that converter.

## Accepted cohort — agent lifecycle and pipe-context cache

Replace the six obligations from `Middleware/Agents/Agent_Specs.cs` with six ordinary xUnit/MTP
facts split by actual product owner. Put the four `Agent`/`Supervisor` lifecycle facts in the
Abstractions test project under `Middleware`; put the two `PipeContextSupervisor` cache facts in
the Core test project under `Agents`.

The lifecycle cases must assert observable terminal state instead of relying on absence of an
exception. Preserve the inherited 50-iteration ready-fault race exposure, but use the runner's
cancellation token rather than local timeout values. The cache cases must prove exact context
identity progression and disposal: both a pipeline failure and explicit invalidation dispose
context `1`, after which the next send uses context `2`. This is the behavior actually asserted by
the inherited code and implemented by the product; the contrary sentence in the frozen ledger is
not copied.

Add four Abstractions and two Core passive requirement-projection rows, add one six-row terminal
disposition, raise the UnitArchitecture floor from 787 to 793, run both focused project gates and
the unfiltered profiles, reject independent one-cause product and requirement mutations, and
delete only the fully replaced inherited file.

The six inherited obligations are terminally mapped one-to-one. Five independent product
mutations reject lost ready faults, broken empty completion, skipped child shutdown, ignored agents
added after readiness, and retained faulted contexts. One scenario mutation rejects omitted explicit
invalidation, and one metadata mutation rejects requirement-projection drift. The final profile
floor is 793.

## Accepted cohort — custom pipe specifications

Replace the two obligations from `Middleware/Authentication_Specs.cs` under their actual
Abstractions source owner `Middleware/Configuration`. Do not retain the misleading Authentication
feature name: the inherited authentication filter is test-owned sample code whose purpose is to
exercise the public custom-pipe-specification contract.

Use one ordinary fact to prove exact ordered execution of the custom filter's selected branch and
the following pipe segment. Use one two-row theory to prove that empty and null configuration both
produce an exact failure result and prevent `Pipe.New` from returning a pipe. Add two passive
requirement rows and one two-row terminal disposition, raise the UnitArchitecture floor from 793 to
796, run the focused Abstractions and unfiltered profiles plus independent one-cause product,
scenario, and requirement mutations, and delete only the fully replaced inherited file.

Both inherited obligations are terminally mapped to two source-owner methods with three execution
cases. Independent mutations reject skipped validation, omitted specification application, a
missing following segment, acceptance of empty roles, and requirement-projection drift before the
final 796-case run.

## Accepted cohort — bound pipe contexts

Replace the single obligation from `Middleware/Bind_Specs.cs` with one deterministic source-owner
fact under Core `Configuration`. The fact must observe the public `UseBind` path, the test-owned
`IPipeContextSource`, `ContextPipe`, the resulting `BindContext<TLeft, TRight>`, and the following
outer pipe segment as one awaited operation.

Assert the exact left-context identity, right-context identity and value, and the exact ordered
trace. Do not reproduce the inherited completion-source timeout or console output. Add one passive
requirement row and one terminal disposition, raise the UnitArchitecture floor from 796 to 797,
run the focused Core and unfiltered profiles plus independent one-cause product, scenario, and
requirement mutations, and delete only the fully replaced inherited file.

The inherited obligation is terminally mapped to one deterministic source-owner fact. Independent
mutations reject missing configured output, lost left and right contexts, missing outer pipeline
continuation, and requirement-projection drift before the final 797-case run.

## Accepted cohort — cache buckets, age, and capacity

Replace the seven obligations from `Middleware/Caching/Bucket_Specs.cs` with five ordinary xUnit
methods and seven execution cases split between the actual Core owners `Caching/Internals/Bucket`
and `Caching/GreenCache`. Keep frozen-clock, within-maximum-age, expired-value, over-capacity, and
usage-aware-value scenarios semantically distinct.

Synchronize asynchronous cleanup with exact cache observer events and the one typed repository
operation timeout. Assert cache statistics, observer additions/removals, non-empty bounded
`GetAll`, and the direct bucket's exact links. Add five passive requirement rows and a seven-row
terminal disposition, raise the UnitArchitecture floor from 797 to 804, run focused and unfiltered
profiles plus independent one-cause mutations, and delete only the fully replaced inherited file.

All seven obligations are terminally mapped to five source-owner methods and seven native cases.
Independent mutations reject broken bucket back-links, disabled expiration and capacity cleanup,
lost removal notifications, a missing usage signal, and requirement-projection drift before the
final 804-case run.
