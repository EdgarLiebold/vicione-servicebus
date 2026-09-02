# Plan — native test reconstruction

## Target

Maintain every meaningful product purpose as a source-owner test under the promoted `tests` tree,
using xUnit 4 on Microsoft Testing Platform 2. The permanent tree mirrors `src`; only
`Architecture`, `Testing`, and `Tools` are non-product owners. No phase name, generic `Core` bucket,
Python test platform, VSTest path, receipt, interceptor, or execution sentinel is part of the design.

## Current profile floors

- `UnitArchitecture`: 2996 currently executed unfiltered cases;
- `LocalIntegration`: 326 predeclared unfiltered cases;
- `SqlServerLocalIntegration`: 60 predeclared unfiltered cases;
- `AzureServiceBusLocalIntegration`: 24 predeclared unfiltered cases.
- `RabbitMqLocalIntegration`: 17 predeclared unfiltered cases.

The floors are accepted lower bounds, not completeness evidence. Each executable project embeds one
durable product-requirement projection and compares it with passive metadata compiled into the same
assembly. The inherited semantic ledger remains the migration input until every disposition is
closed; it is never the runtime test architecture.

## Implementation order

1. Complete one coherent source-owner cohort from product source, accepted native behavior evidence,
   and independently frozen review findings.
2. Keep unit, local-integration, broker/database, and external-resource profiles separate.
3. Run locked restore, Release build, unfiltered MTP tests, bounded formatting, static test-quality
   review, and targeted false-green mutations. Only the Lead starts .NET/MSBuild processes.
4. Commit a stationary accepted cohort before beginning the next one. Remote publication remains a
   separate explicitly authorized operation.
5. Keep the completed native tree free of resurrected inherited projects and remove any directory
   only when it is physically empty.

Cohorts are sized by semantic cohesion rather than by individual file or obligation. Adjacent
behaviors under one source owner should normally share one package-level restore, complete build,
unfiltered profile, formatting pass, mutation review, and commit. As a planning range rather than a
quota, prefer roughly 5–15 source files and 8–40 obligations when the product boundary supports it;
split at source-owner, dependency, execution-profile, transport, or external-resource boundaries.
Single-obligation cohorts are reserved for genuinely atomic behavior or for a final file-closure
blocker.

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

## Accepted cohort — completed node-factory promotion

Replace the single obligation from `Middleware/Caching/NodeTracker_Specs.cs` with one deterministic
fact under its actual Core owner `Caching/Internals/NodeTracker`. Construct one pending value,
`NodeValueFactory`, and temporary `FactoryNode`; add the same factory to the tracker and wait only
for its real observer event using the central typed timeout.

Assert the exact produced value through the temporary and stored nodes, the observer's exact node
and value payload, the promotion to a distinct `BucketNode`, and the complete one-operation
statistics delta. Add one passive requirement row and one terminal disposition, raise the
UnitArchitecture floor from 804 to 805, reject independent promotion/statistics/observer/projection
mutations, and delete only the fully replaced inherited file.

The inherited obligation is terminally mapped to one source-owner fact. Independent mutations
reject reporting a temporary node instead of the stored node, incorrect miss accounting, missing
addition accounting, lost observer output, and requirement-projection drift before the final
805-case run.

## Accepted cohort — cache indexes and pending-value factories

Replace the fourteen obligations from `Middleware/Caching/Tests.cs`, `Index_Specs.cs`,
`MissingValueFactory_Specs.cs`, and `TwoIndex_Specs.cs` with twelve ordinary xUnit facts under the
actual Core source owners `Caching/GreenCache`, `Caching/Internals/Index`, and
`Caching/Internals/NodeValueFactory`. Do not preserve test-helper self-tests as an artificial API;
bind their exact success/fault evidence to the product paths that consume equivalent local
factories.

Use controlled completion sources for pending factories and real `ICacheValueObserver<T>` events
for asynchronous cache propagation. Bound every wait with the central typed operation timeout and
current xUnit cancellation token. Preserve the inherited 100-value multiple-index boundary and
assert exact identity, values, factory call counts, exceptions, cache statistics, both indexes, and
`GetAll` visibility. A failing test that reveals a product defect is not weakened; the product is
corrected minimally and the repaired invariant receives its own targeted mutation.

Create the twelve passive requirement-projection rows and a fourteen-row terminal disposition,
raise the UnitArchitecture floor from 805 to 818, run focused and unfiltered native MTP gates plus
independent one-cause mutations, and remove each inherited fixture only after every row it owns is
terminal. `Tests.cs` support still used by `CacheRecovery_Specs.cs` must first move API- and
behavior-equivalently to a dedicated inherited support file; it is not copied into the new native
test architecture.

The fourteen inherited rows are terminally mapped to twelve source-owner facts plus one independent
pending-removal hardening fact. The final unfiltered profile is 818/818, four one-cause mutations
are rejected, and the complete accepted commit is preserved remotely at `5a60d423`.

## Accepted cohort — internal endpoint-resource cache

Replace the thirteen obligations from `Caching/CacheRecovery_Specs.cs` and
`Caching/Cache_Specs.cs` under the actual product owner `Internals/Caching`. The old cache remains
production-critical for send-endpoint, ActiveMQ, Event Hubs, and Amazon SQS resource ownership, so
this is a native reconstruction rather than a legacy deletion.

Use ten cohesive behavior facts for failure cleanup, exact add/read identity, one- and two-failure
recovery, concurrent single-flight creation, single-use capacity, usage-aware retention,
deterministic distribution metrics, tracker churn, and sequential TTL-cache behavior. Duplicate old
add/read obligations share the exact stronger carrier in the terminal disposition. Add two separate
`NEW_HARDENING` facts for TTL configuration and timestamp-frequency correctness. Every concurrency boundary uses
completion sources and the central operation timeout; no random generator, sleep, polling delay,
elapsed-time assertion, or assertion-free stress test is allowed.

Correct `TimeToLiveCachePolicy` minimally to use one injectable `TimeProvider` for both timestamp
creation and elapsed-time calculation, defaulting to `TimeProvider.System`. Do not change consumer
APIs or transport behavior. Create twelve passive requirement-projection rows, raise the behavior
floor from 818 to 830, and add one architecture fact that prevents the inherited and native
same-named core test projects from sharing an intermediate artifact directory. The resulting
UnitArchitecture floor is 831. Remove both inherited fixtures plus their now-unneeded support files
only after focused, unfiltered, old-project, static-quality, and independent one-cause mutation
gates pass.

The thirteen inherited obligations are terminally mapped, all eight independent mutations are
rejected, and the complete accepted commit is preserved remotely at `37f468ad` with 831/831 cases.

## Accepted cohort — send-endpoint cache consumer

Replace `OBL-R0-CORE-D-0403` from `SendEndpointCache_Specs.cs` with one hermetic source-owner fact
under `Transports`. Resolve two distinct loopback addresses concurrently through a running in-memory
bus, repeat the same concurrent pair against the warm cache, and assert exact per-address identity
plus cross-address separation. Use the central operation timeout and current xUnit cancellation
token; no sleep, polling, exception-only verdict, or test double is allowed.

Create one passive requirement row and one terminal disposition, raise the UnitArchitecture floor
from 831 to 832, reject a direct-factory bypass mutation, and remove the inherited fixture only after
the focused, old-project, unfiltered, static-quality, and mutation gates pass.

The sole inherited obligation is terminally mapped, the direct-factory bypass is rejected, and the
complete accepted commit is preserved remotely at `19e14d90` with 832/832 cases.

## Accepted cohort — raw System.Text.Json object consumption

Replace `OBL-R0-CORE-D-0027` from `ConsumeJsonObject_Specs.cs` with one hermetic source-owner fact
under `Serialization`. Send a local three-property contract through a real running in-memory bus to
a handler registered for `System.Text.Json.Nodes.JsonObject`. Assert the exact camel-case property
set and every typed value. Do not preserve the inherited assertion-free completion, console output,
NUnit lifecycle, or TestFramework message dependency.

Create one passive requirement row and one terminal disposition, raise the UnitArchitecture floor
from 832 to 833, reject an empty-object projection mutation, and remove the inherited fixture only
after the focused, old-project, unfiltered, static-quality, and mutation gates pass.

The obligation is terminally mapped, the empty-body projection is rejected, and the complete
accepted commit is preserved remotely at `908b4e79` with 833/833 cases.

## Accepted cohort — System.Text.Json collections, metadata, scalars, and type safety

Replace the 16 open System.Text.Json obligations from the six inherited array, attribute,
date-time-format, enumerable, property-type, and type-handling fixtures under the `Serialization`
source owner. Reuse the already accepted MessagePack dispositions for the seven independent rows in
the two mixed fixtures; never duplicate those behaviors in the core owner.

Eleven ordinary xUnit methods materialize 13 native cases. They prove literal and semantic null
arrays, exact arrays and `ICollection<T>`, interface `IEnumerable<T>`, duplicate-key list order and
values, interface-property converter metadata, nullable interface collections, ISO timestamp text
and UTC identity, `char`/`char?` boundaries, U+0001 through a private setter, and payload type-marker
safety. The common helper traverses the real System.Text.Json envelope serializer/deserializer and
does not depend on the inherited TestFramework.

All 23 rows owned by the six files are terminally closed: 16 here and seven in the accepted
MessagePack disposition. Three independent one-cause mutations prove attribute copying,
list-versus-dictionary classification, and hostile-marker fixture integrity. The UnitArchitecture
floor is raised from 833 to 846; the six inherited fixtures are removed only after the native
project and remaining inherited project both build cleanly.

## Accepted cohort — object and date-time conversion

Move the three DateTime/DateTimeOffset conversion boundaries to their actual
`Initializers/TypeConverters` owner and the two `JsonElement`/dictionary transformation boundaries
to `Serialization`. Five ordinary xUnit facts assert exact values and invariant round-trip text;
the UTC-MinValue case uses an explicit UTC kind and is therefore independent of the host time zone.

The five inherited obligations are mapped one-to-one. Independent mutations reject loss of the
round-trip format, loss of the concrete `GetObject<T>` fallback, and discarded `Transform<T>` input.
The unmutated project also passes under `ar_SA.UTF-8`. The UnitArchitecture floor is raised from 846
to 851, and the two obsolete NUnit files are removed after the remaining inherited project builds
cleanly.

## Accepted cohort — serialization faults

Replace the three fault obligations from `SerializationFault_Specs.cs` and
`Serialization/DeserializerFault_Specs.cs` with three ordinary xUnit facts under the actual
`Serialization` owner. Run real in-memory endpoints and assert the complete observable boundary:
typed request faults preserve the request and exact consumer exception; unreadable unregistered
content produces a `ReceiveFault` with message, endpoint, host, media type, and serialization
identity; a nested Boolean-to-integer mismatch produces a JSON receive fault and never dispatches.

Use only the central operation timeout and xUnit cancellation token, and retain no sleep, clock
measurement, NUnit lifecycle, inherited TestFramework contract, assertion-free completion, or
generic exception-only verdict. Three independent mutations remove the request failure, replace the
unsupported media type, and repair the nested payload. The UnitArchitecture floor rises from 851
to 854; delete both inherited files only after focused, old-project, unfiltered, formatting,
static-quality, and mutation gates pass.

## Accepted cohort — minimal envelope and delayed redelivery

Replace `MinimalBody_Specs.cs` with one ordinary source-owner fact under `Serialization`. Preserve
the combined contract in one execution: a hand-written System.Text.Json envelope containing only
`message` and `messageType` materializes the local interface contract, its first handler failure is
scheduled for the exact one-hour interval, and virtual in-memory time releases exactly the second
delivery with redelivery counts zero then one.

The test uses the real DI-configured in-memory bus and its registered `IInMemoryDelayProvider`; it
contains no wall-clock measurement, delay, polling, inherited TestFramework message, or NUnit
lifecycle. The first complete-profile run exposed a product race: `MessageQueue` deferred delay
registration to `Task.Run`, so virtual time could advance first. Remove only that unnecessary
thread-pool boundary, retain asynchronous delivery and cancellation behavior, and prove the repair
under the same unfiltered load. Raise the UnitArchitecture floor from 854 to 855 and delete the
inherited file only after exact-interval, redelivery-count, focused-repeat, inherited-build, and
unfiltered gates pass.

## Accepted cohort — Core MessageBody contract closure

Close the remaining 30 obligations in `Serialization/MessageBodyLength_Specs.cs` after composing
them with the 51 accepted Abstractions dispositions and six accepted MessagePack dispositions.
Create source-parallel tests for `MemoryMessageBody`, the three System.Text.Json body types,
`NotSupportedMessageBody`, and the default System.Text.Json serializer options. Add exact concrete
MessageBody type-set facts to the Core and MessagePack owners so the original three-assembly census
is preserved without introducing a cross-module reference into the Core tests.

Each of the four serializing Core body tests executes all four first-accessor orders against exact
external byte and text oracles. Every execution reads the full stream, proves it is read-only,
performs a real rejected write, and rechecks the body after the failure. The envelope is fixed and
clock-independent. The options fact proves compact output is smaller than an externally indented
equivalent and round-trips the exact values. The unsupported body fact requires exact
`NotSupportedException` results from all four members.

Add seven Core projection rows and one MessagePack projection row. Add a self-contained terminal
disposition whose groups cover all 87 inherited IDs without overlap or omission. Raise the
UnitArchitecture floor from 855 to 875. Delete the inherited file only after focused project runs,
the remaining inherited-project build, the unfiltered UnitArchitecture and LocalIntegration
profiles, bounded formatting/static-quality checks, and independent mutations of length, accessor
order, writable-stream, compact-options, unsupported-body, type-census, and requirement projection
have all passed.

The accepted implementation closes all 87 obligations exactly once, adds 20 native cases, and
rejects ten valid one-cause mutations. Both source-owner projects and the remaining inherited
project build cleanly; UnitArchitecture passes 875/875 and LocalIntegration passes 3/3. The fully
replaced inherited fixture is removed.

## Accepted cohort — System.Text.Json contract shapes

Replace eight obligations from four related inherited files in one source-owner package. Seven
ordinary native cases cover immutable constructor-bound request/response messages, exact compact
`decimal.MaxValue` string representation, extension data through envelope and raw JSON, and
declared polymorphism for scalar, array, and list properties. Every messaging case traverses the
real in-memory serializer and receive path and distinguishes a successful message from a
`ReceiveFault`.

The global serializer-options configuration API is mutable product state. Its two configuration
cases therefore run in one non-parallel xUnit collection and restore the original options in an
unconditional nested `finally`; no state leaks into another test. The misleading inherited claim
that constructor parameter names differ is corrected: the names match, and the retained contract is
immutable constructor binding. The formerly ignored `Cost` value is now asserted.

Five one-cause mutations prove decimal-converter registration, per-message serializer options,
declared polymorphism, exact response values, and requirement projection. Raise the
UnitArchitecture floor from 875 to 882 and remove all four inherited files only after the complete
package gates pass.

## Accepted cohort — System.Text.Json application-format compatibility

Replace `OBL-R0-CORE-C-0342`, `-0368`, `-0370`, and `-0372` with four ordinary source-owner facts:
generated Protobuf through envelope JSON, generated Protobuf through raw JSON, and opaque XML
text/UTF-8 bytes through each retained JSON mode. The Protobuf fixture must be a new minimal schema
generated under `tests2/ViciOne.ServiceBus.Tests/Serialization/Protobuf`; it must prove scalar,
getter-only repeated, and timestamp fields and use only a local standard System.Text.Json populate
annotation. Do not add a test-only converter to claim unsupported generated-map compatibility.
Google.Protobuf and Grpc.Tools are test-only dependencies;
neither may enter `src/**`.

Treat `OBL-R0-CORE-C-0473` as resolved by `PO-2026-08-16-01`: raw XML means an opaque application
value transported through the retained serializers, never an XML wire serializer or XML media type.
Reuse the accepted MessagePack dispositions for `-0367`, `-0369`, and `-0371`; do not create second
tests for those already closed identities.

Add four passive Core requirement rows and a terminal source-file closure. Delete both inherited
fixtures and their five private Protobuf support files only after the four open executable rows are
mapped, the remaining inherited project builds, and the accepted MessagePack dispositions are
referenced explicitly. Remove the now-unused Google.Protobuf reference from the inherited project
and refresh only lock files whose evaluated project graph actually changes. Raise the unfiltered
UnitArchitecture floor from 882 to 886.

Acceptance requires locked restore; zero-warning non-incremental Release build; focused and
unfiltered native MTP runs; unchanged LocalIntegration; bounded formatter, assertion-quality,
anti-pattern and pseudo-mutation review; and independent one-cause mutations for missing populate
semantics, false XML media types on both JSON modes, and missing requirement projection. No product
source or public API changes are authorized by this cohort.

All acceptance gates pass. Four source-owner cases replace the four open executable identities and
resolve the cross-format R0 question without reintroducing a wire serializer. The final unfiltered
profile is 886/886, LocalIntegration remains 3/3, both Release builds contain zero warnings and zero
errors, and all four one-cause mutations fail for their intended reason. The seven inherited source
and private support files are deleted only after the old core test project also builds cleanly.

## Accepted cohort — System.Text.Json collection compatibility

Extend the existing source-mirrored `Serialization/SystemTextJsonCollectionTests.cs` with nine
ordinary xUnit methods materializing thirteen new cases. They use the real envelope serializer and
assert exact external values, collection counts, dictionary keys, set membership and sequence order;
the inherited `Equals` implementations and wall clock are not test oracles.

The terminal map is fixed before implementation:

- nested object list: `OBL-R0-CORE-C-0297`;
- empty/single/multiple dictionary: `-0299`, `-0301`, `-0303`;
- concrete and interface sets: `-0305`, `-0319`;
- nested object: `-0307`;
- primitive arrays empty/single/many and the duplicate empty-array identity: `-0309`, `-0311`,
  `-0313`, `-0323`;
- constructor-bound type with a private setter: `-0315`, carried by the existing stronger
  `SystemTextJsonScalarTests.ControlCharacterAndPrivateSetter_RoundTripExactly`;
- read-only dictionary/list graph: `-0317`;
- generic object array: `-0321`;
- empty contract: `-0325`;
- enum: `-0327`;
- ordered key/value list: `-0329`, carried by the existing stronger
  `SystemTextJsonCollectionTests.DuplicateKeyPairs_RemainAnOrderedListWithoutKeyCollapse`.

Add one passive requirement row per native method, create a 34/34 source-file disposition that
composes the 17 pre-existing MessagePack rows with these 17 Core rows, and remove
`MoreSerialization_Specs.cs` only after focused and unfiltered verification. Remove every resulting
empty directory under `tests/`. The predeclared UnitArchitecture floor is 899; LocalIntegration
remains 3. At least four independent one-cause mutations must reject element loss, dictionary-value
loss, constructor-bound private-setter value loss, and sequence reordering, plus
requirement-projection drift.

Outcome: the exact 34-row source closure is terminal; 13 new cases and two stronger existing
carriers cover all 17 open Core identities. The inherited fixture is removed, all resulting empty
`tests/` directories are removed, five independent mutations fail for their intended reasons, and
the accepted floors are UnitArchitecture 899/899 and LocalIntegration 3/3 with no skipped tests.

## Accepted cohort — Core serializer configuration and forwarding

Replace all 14 obligations `OBL-R0-CORE-C-0352` through `-0365` with fourteen ordinary xUnit facts
under `Serialization`. Preserve each validation and callback boundary one-to-one. Put the five
process-global option cases in `SystemTextJsonGlobalOptionsCollection`, save the original options
before each case, and restore the exact instance in `Dispose` even after an assertion or
configuration failure. Use exact validation members and exception types instead of matching broad
exception text.

Replace duplicate forwarding obligations `OBL-R0-CORE-C-0151` and `-0152` with one ordinary fact
using the real in-memory bus. It must consume the concrete input through its interface contract,
forward the same consumed context to a second endpoint, and assert exact fixed identifiers, values,
the concrete-only property, custom header, envelope JSON media type, and one downstream delivery.
The terminal disposition maps both old rows explicitly to this stronger single carrier.

Add fifteen passive replacement-requirement rows, one source-parallel hardening row for the
`PipeExtensions.IsEmpty` defect, and two source-file dispositions. Delete both inherited files
only after the focused tests, native Core project, remaining inherited project build,
UnitArchitecture floor 915, unchanged LocalIntegration 3, bounded formatting, static quality, and
at least five effective one-cause mutations pass. The mutations must independently reject unknown
serializer acceptance, unknown default acceptance, ignored callback replacement, unsafe shared
instance mutation or nulling, lost forwarding metadata/body, unsafe empty-pipe probing, incorrect
empty-pipe classification, and requirement projection drift.
Delete every directory that becomes empty after the source files are removed.

The PO-approved forwarding-expiration correction is part of this cohort's product-defect rule, not
a test workaround. Preserve the source expiration through the caller pipe, accept only a final
positive TTL as an explicit revival, and otherwise discard centrally before transport, persistent
outbox, or mediator dispatch while emitting `FORWARD-EXPIRED`. Keep the response/fault one-second
minimum separate. The forwarding, MessagePack, Outbox, Mediator and request-outcome tests plus the
targeted boundary mutations are mandatory closure evidence.

Outcome: both inherited fixtures are terminally mapped and removed. The native replacements close
all 16 inherited identities, add the source-parallel empty-pipe guard, and retain forwarding over
the real in-memory path. Expiration is enforced consistently at transport, persistent-outbox, and
mediator boundaries. The bounded source-owner projects, complete UnitArchitecture profile at
937/937, and LocalIntegration profile at 3/3 pass with zero skipped tests; all independent one-cause
mutations fail for their intended reasons.

## Accepted product normalization — envelope metadata and time source

The PO selected Option C after the forwarding review. One internal projection in
`ViciOne.ServiceBus.Abstractions` now owns identifiers, addresses, message types, expiration,
sent time, headers and host metadata for every retained wire envelope. System.Text.Json and
MessagePack retain only their payload encoding and mechanical assignment into their wire models.

A pipeline context can carry the standard .NET `TimeProvider`; absence means
`TimeProvider.System`. Envelope materialization captures no more than one UTC instant and adds a
relative TTL exactly, including zero and negative values. The response/fault one-second exception
remains upstream and is not a serializer fallback. The two legitimate forwarding stages remain
separate.

The bounded execution set is the Abstractions, Core and MessagePack test projects. Required closure
is a zero-warning Release build and unfiltered run for each, the complete UnitArchitecture profile,
cross-format metadata comparison, deterministic `FakeTimeProvider` boundaries and four one-cause
mutations covering process-clock substitution, TTL clamping, multiple clock reads and serializer
field drift.

Outcome: the common projector supplies one captured UTC instant and identical envelope metadata to
System.Text.Json and MessagePack. All bounded Release builds and tests pass without warnings or
skips, UnitArchitecture remains 937/937 and LocalIntegration remains 3/3, and all four targeted
mutations are rejected for the intended reason.

## Accepted cohort — Testing observation primitives

Replace `OBL-R0-CORE-C-0373` through `-0381` and `OBL-R0-CORE-C-0415` through `-0418` under the
source-mirrored `Testing` owner. Use ordinary xUnit facts and theories with the public
`SentMessageList` and `AsyncInactivityObserver`; do not recreate the inherited fixture hierarchy.

Normalize both source primitives before writing their final tests. `AsyncElementList<T>` and
`AsyncInactivityObserver` must accept one standard .NET `TimeProvider`, while existing constructors
remain source-compatible and default to `TimeProvider.System`. Both asynchronous and synchronous
list deadlines and every observer interval use that provider. The synchronous iterator must never
hold its message-list monitor across caller-controlled enumeration. The observer may absorb its
own lifetime cancellation but must not hide failures raised while querying a connected source.

The exact behavioral set is immediate Any/SelectAsync, both include variants, exclude, pattern
matching, arrival after observation begins, observation cancellation, virtual timeout, force before
and after task materialization, active-to-inactive transition, and one query per virtual interval.
Add separate hardening for the synchronous virtual deadline and visible source-query failure.
Neither product nor tests may use `Thread.Sleep`, real `Task.Delay`, stopwatch thresholds, polling,
or a custom clock abstraction.

Create one terminal disposition per inherited source file and one passive requirement row per native
method. Delete both inherited files only after focused Release build/test, the unfiltered Core
project, complete UnitArchitecture and unchanged LocalIntegration profiles pass with zero skips.
At least six one-cause mutations must reject process-time substitution in each primitive, broken
later delivery, ignored cancellation, monitor-held enumeration, lost repeated interval queries, and
swallowed source failures. The remaining harness, rolling-timer, saga-polling and recorded-message
time paths stay in the root TODO as a separate path-complete normalization slice.

Outcome: all 13 inherited obligations have terminal source-file dispositions and are carried by 17
ordinary source-owner facts. Both primitives now use one injected standard `TimeProvider`; existing
constructors default to `TimeProvider.System`, synchronous filters execute outside the list monitor,
and connected-source failures remain visible. Eleven independent one-cause mutations reject both
process-time fallbacks, broken delivery and cancellation, monitor-held caller code, lost repeated
queries, swallowed failures, and projection drift. Both inherited fixtures are removed. The native
Core project passes 429/429, the complete UnitArchitecture profile passes 954/954, and
LocalIntegration remains 3/3, all with zero failures or skipped tests.

## Accepted cohort — Testing harnesses, consumers, handlers and sagas

1. Freeze the exact 42-obligation R0 set from the nine remaining Testing fixtures and map every row
   to one stronger native carrier. Treat this only as the inherited lower bound.
2. Normalize the complete affected harness path around one standard `TimeProvider`: direct and DI
   harness budgets, inactivity and rolling timers, observer/list construction, message timestamps,
   saga polling and registration propagation. Preserve source-compatible defaults.
3. Correct the handler failure boundary so observation records the exact exception and the consume
   pipeline still faults. Do not change a test expectation to accommodate the current defect.
4. Build source-mirrored native xUnit/MTP tests under `Testing`, grouped by actual product owner:
   harness lifecycle/DI, consumer harness, handler harness, saga harness/state machine, and shared
   time behavior. Every test owns one harness and uses deterministic coordination or fake time.
5. Add native requirements for every distinguishable positive, negative, fault, lifecycle, time and
   integration behavior absent from the inherited fixtures. Do not overload R0 IDs with new meaning.
6. Run focused Release build and tests, the unfiltered Core project, UnitArchitecture and
   LocalIntegration profiles, then exact source-to-test closure, assertion-quality, anti-pattern,
   gap and smell reviews. Run the final build non-incrementally.
7. Execute one-cause mutations for time-source fallback, handler exception swallowing, missing
   timeout/cancellation enforcement, lost destination/response metadata, lost saga state/fault
   observation, requirement projection and shared-harness leakage.
8. Delete each inherited fixture only after its complete stronger replacement passes. Remove every
   resulting empty directory and record exact terminal dispositions, commands, exit codes and raw
   evidence hashes.

### Accepted subcohort — message observation lists and observers

1. Exercise every public query form of `SentMessageList`, `PublishedMessageList` and
   `ReceivedMessageList` against exact real records: synchronous typed selection, typed predicate,
   configured asynchronous selection, typed asynchronous selection, typed asynchronous predicate,
   configured `Any`, typed `Any`, and typed-predicate `Any`.
2. Exercise `ReceivedMessageList<T>` independently so its covariant typed facade is not inferred
   from the untyped list.
3. Prove that send and publish observers record exact success and exact fault records on the
   configured `TimeProvider`, including identity, context and exception.
4. Prove the shared list owner rejects missing identifiers, keeps the first duplicate identifier,
   propagates filter failures, disconnects failed observations and remains usable afterward.
5. Prove exact empty-sequence behavior of `First`, `FirstOrDefault`, `Count` and `Any`, and exact
   typed and untyped sent-message deconstruction.
6. Reject null provider construction for every concrete list and reject a null required publish
   observer at `TestReceiveEndpointObserver` construction.
7. Prove with a real dynamically connected receive endpoint that its endpoint-local publications
   reach an observer connected only through `TestReceiveEndpointObserver`; synchronize on the
   completed publication and inspect a snapshot so loss fails immediately without a timer.
8. Add one passive native requirement row per test. Run focused tests, complete Core,
   UnitArchitecture and LocalIntegration profiles without skip, then assertion-quality,
   anti-pattern and pseudo-mutation review. Execute independent one-cause mutations for at least
   duplicate suppression, fault recording, configured time, filter failure, the constructor guard
   and the endpoint-local observer connection.

### Accepted subcohort — DI testing utilities and dynamic endpoints

1. Exercise `ConnectPublishHandler<T>` with one rejected and one matching real publication. Preserve
   the exact matching consume context and reject missing harness/filter dependencies before any
   endpoint is connected.
2. Bound dynamic endpoint readiness with the harness `TestTimeout`, `TimeProvider` and cancellation
   token. Do not add a second timer or process-clock dependency.
3. Exercise `AddTaskCompletionSource<T>`, `GetTask<T>` and `GetTasks<T>` with two registrations and
   prove exact task identity, registration order, last-registration resolution and independent
   completion.
4. Exercise both `IReceiveEndpointConnector` overloads supplied by the in-memory DI harness. Prove
   that each callback receives the real registration context, each endpoint consumes only its own
   message, and endpoint-local publications retain exact correlation.
5. Keep implementation classes behind their public interfaces; do not freeze internal composition
   types as new public contracts merely to unit-test them. Add one passive requirement row per
   ordinary test and reject targeted filter/timeout/context/registration regressions.

Outcome: five additional source-owner facts close the filtered publish-handler, bounded readiness,
task-registration and dynamic-connector gaps that the inherited suite never covered. Dynamic
endpoints now apply both global endpoint-callback forms exactly once through either public connector
overload. The test run also exposed and removed a race in the retry assertion by synchronizing on
both terminal fault publication and terminal consume observation. Four independent one-cause
mutations reject filter loss, dependency-guard loss, unbounded readiness and registration-order
drift; the pre-fix dynamic-endpoint run rejects missing global endpoint configuration. The complete
UnitArchitecture profile passes 1046/1046 and LocalIntegration passes 3/3 without skips.

## Accepted cohort — observer pipelines and message-flow diagnostics

The five inherited observer/message-flow fixtures contribute 27 migration obligations. They are a
lower bound only: most merely awaited one callback and several contained no assertion. The native
replacement therefore derives the complete contract from the product path and adds 17 ordinary
xUnit facts: five Abstractions-owned connection/fan-out cases, ten transport/publish/receive/message
observer cases, and two mediator cases. The existing timeline fact is strengthened in place.

The replacement proves exact pre/post/fault exclusivity, message/context/exception identity,
bus-versus-endpoint propagation, independent disconnect, publish-versus-send isolation, handler and
consumer names, receive-versus-consume fault boundaries, independent observer branches, nested
mediator request/response order, and every operation in a deterministic five-message flow. No test
uses a sleep, wall-clock oracle, random input, inherited fixture, or global endpoint convention.

`Connectable<T>` now returns a defensive public snapshot without allocating on internal dispatch,
rejects every missing callback consistently, and turns synchronous or null-task observer failures
into awaited faults while still starting every callback in the stable snapshot. Eight independent
one-cause mutations reject snapshot exposure, incomplete fan-out, lost send/publish/receive/message
or mediator semantics, and omitted timeline records. Release builds have zero warnings and errors;
Core passes 533/533, Abstractions passes 139/139, UnitArchitecture passes 1063/1063, and
LocalIntegration passes 3/3, all without skips. Only then are the five inherited files removed.

## Accepted cohort — message identity, conversation, time and headers

The 15 inherited obligations are terminally mapped to 17 source-derived native facts. The expanded
contract binds complete correlation priority, empty identities, explicit topology and caller
overrides, root and inherited conversations, initiator/source metadata, exact conversation restart
headers, exact `MessageId`-derived UTC time, independent versus shared header storage, header mutation
boundaries, complete interface-object round trips, and converter-independent interface materialization.

The source review exposed an inherited product defect in the independent `DictionarySendHeaders`
constructor: it copied the input and then inserted the same keys again. The correction removes only
the impossible second insertion; the explicit shared-storage mode remains intact. Eleven one-cause
product mutations fail for their intended reasons. Release builds have zero warnings and errors;
Core passes 550/550, UnitArchitecture passes 1080/1080, and LocalIntegration passes 3/3 without skips.
Only then are all five inherited fixtures removed. The exact disposition and product-path analysis
are under `evidence/native-tests/message-identity-and-headers`.

## Accepted cohort — message contexts and dynamic contracts

Treat the 30 obligations in `MessageContext_Specs.cs`, `ContainedMessage_Specs.cs`,
`InterfaceProxy_Specs.cs`, and `DynamicProxySerialization_Specs.cs` only as the inherited lower
bound. Read the complete request/response, send/publish, initializer, dynamic-implementation,
System.Text.Json, MessagePack, and metadata paths before the first test edit. Consolidate duplicate
assertions, replace assertion-free delivery checks with exact observable outcomes, and add missing
validation, failure, type-shape, cache, inheritance, and serializer boundaries derived from the
product.

Place context and request/response behavior under `Contexts`; place emitted-contract construction
under `Internals/Reflection`; keep serializer-specific round trips under `Serialization` in their
own source-owner projects. Use real in-memory dispatch where transport context is the contract and
direct deterministic tests where the implementation builder is the owner. Do not use the inherited
fixture hierarchy, blocking waits, wall-clock timing, random oracles, or metadata-only delivery
claims. A reproduced product defect is corrected in product code and protected by a focused
regression plus a one-cause mutation; tests are never weakened to preserve the defect.

The 30 inherited obligations are terminally mapped to 24 new source-derived Core cases plus the
existing MessagePack interface round trip. The permanent tests prove complete send/request/response
causation, an independent response subscriber, exact timeout and caller-cancellation semantics,
accepted response types, dynamic nested/generic interface transport, every retained serializer,
collectible proxy caching, complete custom attributes, and fail-fast validation of unsupported or
ambiguous contracts.

Source-derived testing exposed four product defects: invalid interface shapes leaked late emission
exceptions, named custom-attribute arrays were passed with the wrong CLR value type, and an already
canceled request lost the caller's cancellation token; request deadlines also bypassed the standard
`TimeProvider` testability boundary. The product paths are corrected and eight
one-cause mutations reject regression. Core passes 574/574 and UnitArchitecture passes 1104/1104,
both without failures or skips. The exact disposition and analysis are under
`evidence/native-tests/message-context-and-dynamic-contracts`; only after final static and profile
verification are the four inherited fixtures removed.

## Accepted cohort — request clients, response matching, mediator, and multibus

Treat the 40 obligations in `RequestClient_Specs.cs`, `RequestClientNew_Specs.cs`,
`RequestFilter_Specs.cs`, `ResponsePatternMatching_Specs.cs`, `MultiBusRequest_Specs.cs`, and
`MediatorRequest_Specs.cs` only as migration evidence. Read every file and the complete connected
client-factory, request-handle, response-handler, response-pattern, mediator, multibus, outbox,
fault, cancellation, filter, and expiration path before designing replacements.

Consolidate duplicate syntax demonstrations into behavioral contracts. Derive the full positive,
fault, timeout, cancellation-race, accepted-type, pattern-matching, TTL/expiration, outbox, scoped
filter, mediator, and bus-isolation matrix from product source. Use virtual time for every deadline,
real hermetic in-memory dispatch for transport causation, and direct unit tests only where a pure
response wrapper or matcher is the actual owner. Preserve no inherited fixture, blocking wait,
wall-clock timer, assertion-free delivery claim, or implementation accident. Fix reproduced product
defects at their source before accepting the corresponding test.

Delete the six inherited files only after exact 40/40 terminal disposition, stronger source-derived
coverage, focused and unfiltered Release profiles without skips, bounded formatting/static review,
and one-cause mutations of the protected request-client boundaries.

The cohort is terminal. All 40 inherited identities have exact dispositions against 24 permanent
source-owner methods; the six inherited fixtures are removed. The retained capability set is
offered through the A+ ViciOne.ServiceBus API, without a MassTransit backward-compatibility
requirement. Source-derived tests additionally cover three-response arbitration, deterministic
deadline/cancellation races, deadline/TTL independence, dependency-injection time ownership,
duplicate response rejection, public null boundaries, accepted-type matching, complete response
context delegation, exact remaining response/fault TTL, shared outbox bypass, scoped filter faults,
and secondary-bus isolation.

Five product boundaries were corrected at their owner: client deadline and transport TTL are
independent; all deadline/TTL calculations use the context-owned `TimeProvider`; public request and
response construction fails early for missing dependencies; multi-response wrappers delegate the
complete context and select deterministically; and the request outbox bypass is centralized in the
shared request-send endpoint. Eleven one-cause mutations reject regression. Core passes 614/614,
Abstractions passes 150/150, UnitArchitecture passes 1155/1155, and LocalIntegration passes 3/3,
with zero failures or skips; Release builds have zero warnings and errors.

## Accepted cohort — type relationships and readable-property reflection

1. Replace the inherited generic-type API with intent-revealing operations for interface matching,
   any closed-generic match, deterministic complete match enumeration, exactly-one match and exact
   generic arguments. Do not retain aliases or forwarding overloads for MassTransit compatibility.
2. Make the generic-match cache an internal implementation detail, store a non-null result object,
   and sort multiple interface matches by stable type identity. Exactly-one callers must fail on
   ambiguity instead of consuming reflection order.
3. Rename property helpers to state that they return readable instance/static properties. Traverse
   base types and inherited interfaces before derived declarations, de-duplicate diamond paths and
   select each property by its own getter. Update all product consumers atomically.
4. Fix runtime future registration at its source so non-state-machine and wrong-state-machine types
   fail directly as `futureType` argument errors. Add a positive valid-registration case as well as
   both negative cases.
5. Build source-mirrored native tests under Abstractions `Internals/Extensions` and Core
   `Configuration/DependencyInjection`. Derive null, invalid-definition, open-source, nested-base,
   multiple-interface, ambiguity, task-result, hiding, indexer, write-only, static and diamond
   boundaries from product source; treat the 37 inherited rows only as minimum evidence.
6. Create exact terminal disposition for all 37 identities. Delete the four inherited fixtures only
   after their complete replacements pass; keep the separate legacy helper files until their other
   consumers are migrated.
7. Reject one-cause mutations for ambiguity handling, deterministic matching, invalid generic
   definitions, write-only filtering, derived-interface precedence, future-type validation and
   requirement projection. Then run focused tests, zero-warning Release builds, UnitArchitecture,
   LocalIntegration and the full retained product-source solution without skips.

The cohort preserves every useful generic-type, interface and readable-property feature behind an
intent-revealing ViciOne API; MassTransit compatibility aliases are absent. All 37 inherited rows
have exact terminal dispositions. The new cache design does not root collectible metadata. The four
inherited fixtures are removed only after the final Abstractions 191/191, Core 618/618,
UnitArchitecture 1200/1200 and LocalIntegration 3/3 gates, all without skips. Every retained product
project builds in Release with zero warnings and errors. Thirteen independent one-cause mutations
reject the protected behavior, resource-lifetime and requirement-projection regressions.

## Accepted cohort — middleware coordination and resilience

Treat the 19 inherited identities in the eleven middleware fixtures as a lower bound. The complete
latest-value, one-time setup, cancellation, fork/join, nested-pipe, partition, rate/concurrency,
observer, rescue, retry and circuit-breaker product paths define the retained behavior. Preserve
features behind an intent-revealing ViciOne API; do not preserve fixture shapes or accidental public
implementation states.

Use virtual time for rate and circuit behavior, fresh keys and concurrent callers for arbitration,
and exact exception/identity/order assertions. Public routing key providers reject null. Setup and
limit adjustment recover after terminal failure or cancellation. The inherited circuit shape
retained by this historical migration cohort is superseded by the greenfield circuit-breaker cohort
below; its timer and router-event ownership are not current architecture.

The cohort is terminal: all 19 identities have exact replacement dispositions, the eleven inherited
files are removed, seven independent one-cause mutations reject the protected behavior, and the
post-deletion inherited project still compiles. Abstractions passes 201/201, Core 643/643,
UnitArchitecture 1235/1235 and LocalIntegration 3/3 without failures or skips; all affected Release
builds complete with zero warnings and zero errors.

## Accepted cohort — middleware routing, limits and scope

Treat the eight inherited identities in the circuit, payload-scope, concurrency, dispatch and
dynamic-router fixtures as migration evidence. Read the complete parent/local payload, converter,
output/tee, type/key routing, limiter and circuit-state paths before replacement. Preserve useful
capabilities behind fail-fast ViciOne boundaries, not the old fixture or reflection failure shapes.

Source-owner tests prove parent-readable/local-write payload isolation, nested scopes, typed fan-out,
exact route disconnection, matched and unmatched dispatch continuation, keyed selection, invalid
collaborators/converter results, the exact configured concurrency maximum and the default circuit
activation threshold. Eight one-cause mutations cover those boundaries.

The cohort is terminal: all eight identities have exact dispositions, the five inherited fixtures
are removed, and the remaining inherited Core project still compiles. Abstractions passes 203/203,
Core 649/649, UnitArchitecture 1243/1243 and LocalIntegration 3/3 without failures or skips; the
final Engineering build completes with zero warnings and zero errors.

## Accepted cohort — greenfield circuit breaker

Replace the inherited public runtime-state API with one public validated options boundary and one
internal timer-free state machine. Capture an immutable settings snapshot when the pipe is built.
Closed sampling uses the configured standard `TimeProvider`; minimum throughput and failure ratio
are inclusive. Open rejects before the protected pipe with `CircuitBreakerOpenException`. At the
exact duration boundary, one compare-and-swap winner owns the only half-open probe; competitors fail
immediately. Success closes and resets backoff, a classified failure reopens, while caller
cancellation and unclassified failures release the probe without claiming either success or
failure. Retain exception filtering and retry/concurrency composition. Remove router events, public
states and timers. Emit only low-cardinality metrics and activities under the existing
`ViciOne.ServiceBus` OpenTelemetry source.

Twenty-three ordinary xUnit/MTP facts own configuration and behavior. They cover exact and
just-below thresholds, sampling/open boundaries, a filter snapshot isolated from retained builders
and caller-owned arrays, bounded backoff, cancellation causality, a real 33-caller CAS race,
classifier-failure recovery, measured retry/concurrency composition, public surface, exact
low-cardinality OpenTelemetry signals and no-throw observer isolation. The predeclared
UnitArchitecture floor was 1494 at acceptance; focused unfiltered Unit and LocalIntegration
profiles, the Engineering Release build and the one-cause mutations were also green.

## Accepted cohort — middleware retry

Treat the 17 inherited retry identities as a behavior lower bound. Read the complete retry filter,
policy/context, configuration, cancellation, observer, nested-payload, typed-dispatch and general
task-executor paths before replacement. Preserve capabilities, not the former API or its private
command-specific fixture hierarchy.

Use iterative retry loops, exact terminal failures and tokens, context-owned or explicitly supplied
`TimeProvider` instances, immutable validated schedules, and one downstream retry owner across
typed dispatch. Cover nested observer ownership at both the owning and outer layers. Remove the
unused message-specific direct retry extension because its capabilities are already owned by the
general task executor and the configured message-retry middleware.

The 54 source-mirrored xUnit/MTP cases raise the predeclared UnitArchitecture floor from 1243 to
1297. Delete the inherited retry fixture and its seven exclusive support files only after the
17-row disposition is complete, focused tests and one-cause mutations pass, and the remaining
inherited Core project still compiles. Finish with unfiltered Core, UnitArchitecture and
LocalIntegration gates plus the complete Engineering Release build.

The cohort is terminal. All 17 inherited identities have exact executing dispositions; the eight
exclusive inherited files and the unused direct message-retry duplicate are removed. Nine
one-cause product mutations fail for their intended reasons. Abstractions passes 203/203, Core
703/703, UnitArchitecture 1297/1297 and LocalIntegration 3/3 without failures or skips. All bounded
and complete Release builds finish with zero warnings and errors, and the bounded .NET whitespace
format check passes.

## Accepted cohort — message and host retry integration

Replace the 17 remaining obligations from `HostConfigurationRetry_Specs.cs`, `Retry_Specs.cs` and
`TypeCastRetry_Specs.cs` as one cohesive integration slice. The previously accepted retry engine is
the implementation foundation; this cohort proves its public configuration, bus lifecycle,
transport-host and polymorphic dispatch boundaries rather than rebuilding policy mechanics.

Use one retry executor and one configured delay. Host send retry accepts an explicit
`TimeProvider`, preserves the exact caller cancellation token, reports transport stopping as a
`ConnectionException`, gives stopping precedence when both sources cancel and rethrows the exact
terminal transport failure. Message retry preserves inner-policy ownership, exact consumer and bus
budgets, disjoint exception-policy ownership, concrete-base and interface dispatch for a concrete
message with an abstract ancestor, explicit/default no-retry behavior, and real cancellation of a
pending retry during bus stop. Invalid public collaborators and policy results fail at their owning
boundary.

Nineteen source-mirrored xUnit/MTP cases raise the predeclared UnitArchitecture floor from 1297 to
1316 and the Core project from 703 to 722. Delete the three inherited fixtures only after their
17-row terminal disposition, focused tests, one-cause mutations, zero-warning Release build,
unfiltered Core and UnitArchitecture runs, unchanged LocalIntegration run, inherited Core compile
check, projection validation, bounded formatting and static test-quality review all pass.

The cohort is terminal. All 17 inherited identities have exact executing dispositions, the three
fully replaced fixtures are removed, and four independent one-cause production mutations fail for
their intended reasons. Core passes 722/722, UnitArchitecture passes 1316/1316 and LocalIntegration
passes 3/3 without failure or skip. The native UnitArchitecture build, remaining inherited Core
project and complete Engineering solution build in Release with zero warnings and errors.

## Accepted cohort — configuration composition and validation

Treat the 29 obligations in the eight inherited configuration fixtures as a lower bound. Read the
complete observer, send/publish composition, saga discovery/creation, runtime-instance and invalid-
configuration paths before replacement. Preserve capabilities behind coherent ViciOne APIs; do not
preserve delayed observer callbacks, reflection order, assertion-free smoke tests or obsolete public
implementation types.

The cohort is terminal. Twenty-eight new inherited-replacement cases plus six source-derived
hardening cases cover stable composition order, validation snapshots, observer re-entry/failure,
late root configuration, failed-initialization recovery, saga role precedence and construction,
runtime instance identity and exact invalid boundaries. All 29 inherited identities have terminal
executing dispositions; the eight fixtures and their empty directory are removed. Ten independent
one-cause product mutations fail for their intended reasons. Abstractions passes 213/213, Core
746/746, UnitArchitecture 1350/1350 and LocalIntegration 3/3 without failure or skip; the remaining
inherited Core project and complete Engineering solution build in Release with zero warnings and
errors.

## Accepted cleanup — test-local message-group sample

The three cases in `Groups/Group_Specs.cs` test only types and extension methods declared in that
same inherited file. They exercise no ViciOne.ServiceBus product code and are not the similarly
named batch-grouping feature. All three identities are terminally classified as non-product and the
file is removed without creating a test of copied test code. The UnitArchitecture floor remains
1350.

## Accepted cohort — pipe-context failure precedence

Replace the five inherited cleanup-precedence cases at their `Agents/PipeContextSupervisor` source
owner. The operation result is authoritative: cleanup after successful delivery cannot create a
retry signal, and cleanup after failure cannot replace the exact primary exception. Fault
notification, stop and disposal remain ordered and independently attempted.

Eight native cases close the five inherited identities and add source-derived coverage for exact
exception identity, lifecycle order, caller-token propagation and failed asynchronous context
acquisition. Eight one-cause product mutations reject regression. The inherited fixture is removed;
Core passes 754/754, UnitArchitecture 1358/1358 and LocalIntegration 3/3 without failure or skip.
All final Release builds have zero warnings and errors.

## Accepted cohort — timeout and cancellation

Replace the four inherited timeout and cancellation obligations at their middleware and
configuration owners. The inherited tests are a lower bound: cover virtual deadline ownership,
nested caller-token normalization, independent cancellation, the complete consume lifecycle,
timer disposal, timeout fault projection, transport-stop suppression and all timeout configuration
projections.

Use the context `TimeProvider` by default and permit an explicit configured provider. Applied pipe
specifications are immutable snapshots. Preserve exact causal exceptions and deterministic caller,
deadline and transport-stop precedence. Keep `TimeoutFilter`, `UseTimeout` and
`ITimeoutConfigurator` public while hiding specification, observer and proxy implementation types.
Reject nonpositive durations in every projected scope and require the configuration callback.

The cohort is terminal. Thirty native cases close all four inherited identities and add the full
source-derived boundary set. The two inherited fixtures are removed. Twelve one-cause product
mutations fail for their intended reasons. Core passes 784/784, UnitArchitecture 1388/1388 and
LocalIntegration 3/3 without failure or skip. All final Release builds and bounded formatting and
static closure checks pass with zero warnings or errors. The complete gate also exposed and fixed a
pre-existing saga-test observation race without changing product behavior.

## Accepted cohort — in-memory delay and scheduled publish

Replace the five inherited delay-provider identities at the `InMemoryTransport` owner and keep the
real scheduled-publish integration boundary. Replace the channel/reader-task/wall-clock-offset
implementation with an injected standard `TimeProvider`, one timer and a comparer-correct ordered
deadline set. `Advance` must be applied when it returns; cancellation removes its entry and preserves
the exact caller token; disposal cancels pending work and releases all resources.

Use `TimeSpan` and `DateTimeOffset` as the public time shapes and do not retain redundant integer or
ambiguous `DateTime` overloads for compatibility. Preserve direct delayed-delivery registration in
`MessageQueue`: no `Task.Run` may allow logical time to advance before registration. Tests must be
source-mirrored, contain no elapsed-time verdict, and cover boundaries, equal and subsequent
deadlines, cancellation, disposal, timer count, maximum supported timer interval, API shape and the
real DI scheduler path.

The cohort is terminal. Eighteen native cases close all five inherited identities and add the full
source-derived set. Fourteen one-cause product/projection attacks fail for their intended reasons.
Core passes 802/802, UnitArchitecture 1406/1406 and LocalIntegration 3/3 without failure or skip. The
two inherited fixtures are removed and all final Release builds and bounded formatting checks pass.

## Bus health waiting

Normalize the complete `BusControlHealthExtensions` owner before migrating the larger health and
kill-switch scenarios. Rename the asynchronous public surface with the `Async` suffix and return the
complete `BusHealthResult`. Add one standard-`TimeProvider` execution path plus system-time
convenience overloads; keep the polling interval private. A finite timeout must either return the
exact expected result after a final boundary observation or throw `BusHealthStatusTimeoutException`
with the last result. Infinite waiting remains available through caller cancellation.

Test the owner under `tests2/ViciOne.ServiceBus.Abstractions.Tests` with ordinary xUnit/MTP cases for
immediate and delayed success, exact-deadline success, zero/finite/infinite timeout, exact
cancellation, validation, empty and invalid collections, concurrency and stable input ordering.
Update every still-compiling product and inherited-test caller mechanically to the renamed API; do
not change their asserted behavior. Add passive requirement rows and prove at least the process-time,
silent-timeout, cancellation-order, final-boundary and collection-order defects with one-cause
mutations. Run focused Abstractions, unfiltered Abstractions, complete UnitArchitecture,
LocalIntegration, remaining inherited Core and Engineering Release gates before accepting C31.

Outcome: the 22-case source-owner cohort passes in full and each case has an exact passive
requirement carrier. Twelve isolated one-cause mutations reject process-time polling, silent
timeouts, cancellation-order loss, missed deadline semantics, invalid collection handling,
reordering, repeated enumeration and projection drift. Abstractions passes 235/235,
UnitArchitecture 1428/1428 and LocalIntegration 3/3 without failure or skip. Every affected caller
and the complete Engineering solution build in Release with zero warnings and errors. The larger
kill-switch state machine remains a separate cohort; no inherited fixture is falsely declared
replaced here.

## Kill-switch lifecycle and recovery

Replace the imported public runtime-state graph with one internal, lifecycle-owned state machine.
Preserve bus-wide and endpoint-local configuration, activation population, failure-ratio threshold,
tracking window, exception filtering, endpoint pause/restart, routing-slip observation and health
transitions. Do not preserve public coordination interfaces, public state classes, the bypassed
restart state, raw timers, process time, mutable shared options or detached transition tasks.

The public configuration uses an explicit ratio in the inclusive range zero through one, an explicit
`TimeSpan` restart delay and one standard .NET `TimeProvider`. Configuration is snapshotted before it
is shared. The runtime performs counter reset and threshold evaluation under one synchronization
boundary, trips at the exact activation population, owns one observable recovery task, retries
failed stop/start operations with bounded delay and cancels recovery when the endpoint is stopped by
its host. Recovery establishes its own log/instrumentation context.

Build the source-derived deterministic Core matrix before deleting the three Core inherited
fixtures. It must cover all public validation, concurrent fault collapse, exact virtual-time
transitions, failure retries, external-stop cancellation, health and instrumentation. Retain the
ActiveMQ and RabbitMQ fixtures for separate real-broker LocalIntegration cohorts.

Outcome: the cohort is terminal. The product now owns one internal synchronized state machine, an
immutable configuration snapshot, standard `TimeProvider`, one observable recovery task, bounded
pause/start retry, terminal cancellation and an explicit `VerifyingRecovery` phase. Twenty-six
ordinary native facts and fifteen one-cause mutations cover the complete source-derived boundary.
The three Core fixtures are removed; ActiveMQ and RabbitMQ remain. Core passes 828/828,
UnitArchitecture 1455/1455 and LocalIntegration 3/3 without failure or skip. The complete serial
Engineering Release build has zero warnings and zero errors.

## Fault diagnostics and host metadata

Replace the two inherited exception-data cases and the inherited host-metadata round trip at their
actual source owners. Fault construction must always detach serializable string-keyed non-null
diagnostic data from the mutable source exception, use ordinal case-insensitive lookup and give
explicit application-wrapper values precedence without replacing the reported inner exception
identity. Remote exception identities and nested chains remain intact.

`BusHostInfo` remains the wire DTO but has one parameterless serializer constructor and one internal
current-process factory; no boolean pseudo-mode or unused version discovery remains. The process
snapshot uses modern runtime APIs and the host cache owns exactly one current and one empty object.
Tests cover direct snapshot boundaries, public shape, real fault publication and real host metadata
serialization. Do not accept publish-observer values as transport-roundtrip evidence.

Outcome: six fault-diagnostic cases, four host-capture cases and one real host transport case pass.
Core is 835/835, Abstractions is 239/239, UnitArchitecture is 1466/1466 and LocalIntegration is 3/3.
Seven isolated one-cause mutations are rejected. The two inherited fixtures are removed only after
all three R0 obligations are replaced. Every final Release build, including Engineering, completes
with zero warnings and errors.

## Initializer duplicate-path consolidation

Do not create redundant native tests when an inherited obligation is already strictly contained by
accepted source-owner tests. `OBL-R0-CORE-D-0415` is jointly covered by the exact scalar-copy test and
the real anonymous-values interface transport test. Record the two-carrier mapping and remove only
the inherited `SendProxy_Specs.cs`; product code, native tests and the 1466 profile floor do not
change.

## InMemory receive-endpoint concurrency

Replace `OBL-R0-CORE-D-0465` at the real endpoint boundary, not with an isolated executor test. One
case proves that a configured limit of one hundred admits all one hundred deliveries before any is
released. A second source-derived hardening case separates prefetch from concurrency and proves that
delivery beyond the configured limit waits for a slot. Both cases use observable barriers rather
than elapsed-time or thread-count assertions.

The one-cause mutation makes the transport ignore `ConcurrentMessageLimit` and use `PrefetchCount`;
the cap case must fail with an observed maximum of four instead of three. Remove the inherited
fixture only after both ordinary cases and the restored product path pass. The UnitArchitecture
floor becomes 1468.

## Structured bus-probe endpoint inventory

Replace `OBL-R0-CORE-D-0199` against the structured `ProbeResult` produced by the real bus. Do not
recreate the old `ViciOne.ServiceBus.TestFramework.GetReceiveEndpointAddresses` helper, which
serializes the probe to JSON and reparses path strings. Prove that the harness input endpoint, the
internal bus endpoint and a dynamic endpoint each appear exactly once, then prove that stopping the
dynamic endpoint removes only that address.

The one-cause mutation renames the InMemory receive-transport `address` probe field and both tests
must fail at the missing structured key. Remove the inherited fixture only after restoration and a
green focused run. The UnitArchitecture floor becomes 1470.

## InMemory outbox fault isolation

Replace `OBL-R0-CORE-D-0294` at the actual in-memory outbox boundary. A handler must await a deferred
response and then fault; the request fault is the causal completion barrier. The test must prove the
exact sent `Fault<T>` and the absence of the deferred response from the harness snapshot, without an
elapsed-time absence window.

Mutate only the filter catch path from `DiscardPendingActions` to `ExecutePendingActions`; the
response-presence assertion must fail. Mutation restoration is accepted only after restoring the
complete try/catch branch, proving the product file byte-identical to its baseline and forcing a
non-incremental rebuild. Remove the inherited fixture only after the restored focused test passes.
The UnitArchitecture floor becomes 1471.

## Native test source-layout enforcement

Turn the agreed source-mirrored test structure into a fail-closed architecture rule. Evaluate every
native project's real `Compile` items and `RootNamespace` through MSBuild, parse each source with
Roslyn and require exactly one top-level namespace matching the project root plus its physical
folder. Keep specialized analyzer and Roslyn support assemblies in the common test-infrastructure
API space by declaring their intentional `RootNamespace` explicitly.

Attack the rule with one otherwise valid source under `Serialization` whose namespace names a
different folder. Only the new architecture fact must fail with the exact path and expected/actual
namespaces. Remove the probe, rerun the complete architecture project, then enforce a 1472
UnitArchitecture floor.

## Message journal

Replace the inherited `Audit` feature as one product slice with four internal phases. First build
the Core contract and observers, then migrate EF Core, then Azure Table, and only after the combined
closure remove every old `Audit` file and fully replaced inherited fixture. There is no public API
compatibility requirement, but no useful capability may disappear.

The Core API is `MessageJournal` throughout. It observes the actually serialized envelope so
transactional outbox and scheduler paths cannot expose internal wrapper types as the journal
message. A caller must explicitly connect a store, an entry-selection/redaction policy, finite
limits and a finite write timeout. No implicit policy captures a payload. Terminal observer
callbacks produce explicit operation and outcome values. A timeout or any policy, provider or OTel
observer failure must leave the original send, publish or consume result unchanged.

The immutable entry owns a version-7 identifier, UTC observation time, operation, outcome, declared
data classification, selected metadata and optional sanitized content. The policy is the sole
owner of inclusion and redaction; providers receive no raw transport context or CLR message.
OpenTelemetry uses one existing ViciOne meter/source identity and low-cardinality operation,
outcome and failure-reason tags only. No log, event, queue, retry, audit query or A09 record is added.

EF Core keeps configurable table/schema persistence and supported PostgreSQL, Azure SQL and SQL
Server behavior. Azure Table keeps service-client and table-client composition and a bounded,
explicit partition strategy. Both enforce finite entry size, count and age, use cancellation, and
persist the same canonical entry semantics. Count and age maintenance runs transactionally with
every append; it has no independent scheduler, background queue or retry path and therefore never
runs for an unconfigured or idle journal.

Acceptance requires normal xUnit 4/MTP v2 tests for default-off composition, exact serialized
envelope capture including outbox, filtering/redaction, metadata and payload fidelity, success and
fault outcomes, timeout/cancellation, provider/OTel failure isolation, immutable snapshots, all
validation boundaries and exact low-cardinality telemetry. Provider suites must prove retention,
capacity, collision-free keys, supported relational variants and both Azure client composition
paths. Every one of the seventeen R0 purposes receives one terminal disposition. Targeted one-cause
mutations must reject raw-payload defaults, CLR-wrapper type capture, outcome loss, operation-impact
on journal failure, missing bounds, retention bypass and telemetry leakage. Final UnitArchitecture,
LocalIntegration, applicable External and Engineering Release profiles pass without failures,
skips, warnings or a second verdict path.

## Current cohort — RabbitMQ address model

Reconstruct the complete inherited `RabbitMqAddress_Specs.cs` cohort as one hermetic transport-owner
slice under `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests`. The 46 R0 obligations are
a lower bound, not the target design. Native tests cover host and endpoint parsing, canonical
rendering, short forms, virtual-host and entity encoding, default and secure ports, credentials,
temporary queues, receive settings and every supported query option. Negative and boundary cases
come from the complete product path, not from the NUnit fixture.

The public address values become immutable Greenfield value objects with correctly named schemes,
defensive collection ownership and fail-fast validation. URI parsing must never truncate a password
at a later colon. Direct endpoint construction enforces the same entity-name contract as URI
construction. Configuration errors expose stable argument or RabbitMQ-address exceptions; tests do
not preserve silent fallback or partial parsing as a desired contract.

Acceptance is one signed xUnit 4/MTP v2 executable project in the source-mirrored transport folder,
one passive embedded requirement projection, a terminal one-to-one disposition of all 46 R0 IDs,
focused locked restore/build/test, the unfiltered UnitArchitecture profile and targeted one-cause
mutations for default-port normalization, virtual-host encoding, direct-constructor validation,
credential preservation, option mapping and defensive snapshot ownership. Only then remove the old
fixture and any resulting empty directory. No broker, container, network or wall-clock oracle is
allowed in this cohort.

The independent reviews found and the correction closes the wider product-path defects: query
values retain every byte after the first separator, opaque short names decode symmetrically, queue
TTL is a numeric AMQP argument, bus topology resolves against the final configured host, and every
direct-constructor name input owns the same validation boundary. Public and formatter-produced
topology names are data and can no longer be reinterpreted as URI options or fragments. The
telemetry test barrier also proves the exact idle deadline rather than merely observing an arbitrary
timer change. The focused transport executable now contains 108 unfiltered cases and the
UnitArchitecture floor was 1822 when this cohort was accepted; the current floor is declared once
at the top of this plan and enforced consistently by the public command owners.

## Current cohort — native core pipeline closure

Work from the clean accepted product baseline `fb90c8e841cc7c5348193bba8105ea3543b45c01`.
This is one source-derived xUnit 4/MTP 2 cohort. It must not recreate the NUnit fixture architecture,
the discarded Python policy system or a second verdict path.

1. Freeze a machine-readable 34-row disposition for `OBL-R0-CORE-B-0426` through `0459`. Each row
   names exactly one terminal replacement carrier and states why it is equal or stronger. Existing
   native tests are reused where the research section names them; they are not copied.
2. Add the missing source-owner tests:
   - `Consumers/DynamicConsumePipeConnectionTests.cs` for consumer factory, object instance,
     multi-message consumer, handler and disconnect behavior;
   - `Testing/ConsumeObserverTests.cs` for exact observer lifecycle and error identity;
   - `Middleware/ContextFilterTests.cs` for true, false, asynchronous and invalid delegate results;
   - `Configuration/Configuration/ConsumerMessageConfigurationTests.cs` for the consumer,
     message and consumer-message layers on both factory and instance registration;
   - extend the send configuration owner and add the publish owner for exactly-once concrete/base
     application without transport or wall-clock involvement;
   - `Configuration/PartitionMessageConfigurationTests.cs` for explicit correlation convention and
     missing-convention failure, while the existing `PartitionerTests` retain concurrency ownership;
   - `Configuration/TransactionConfigurationTests.cs`, `Middleware/TransactionFilterTests.cs` and
     `Contexts/TransactionContextExtensionsTests.cs` for exact options, ownership, commit, rollback,
     original error, fresh retry context, ambient-scope flow and argument boundaries.
3. Write each failing source-derived boundary test before its minimal product correction. Normalize
   handler, context-filter and transaction argument contracts; require actual cancellation request
   before retry treats a token-bearing `OperationCanceledException` as cancellation. Introduce only
   the smallest internal transaction-context factory necessary to observe exact options and
   lifecycle without replacing `System.Transactions`.
4. Use `TestContext.Current.CancellationToken` and the central `OperationTimeout` only as fail-fast
   guards. Use `TaskCompletionSource` with asynchronous continuations and explicit release in
   `finally` for coordination. No `Thread.Sleep`, `Task.Delay`, stopwatch, random scheduling,
   absence-until-timeout or shared running harness is an assertion oracle.
5. After focused tests pass, reread every changed assertion and production branch. Perform targeted
   test-gap, assertion-quality, anti-pattern and smell reviews. Execute one-cause mutations for each
   new product correction and for the 34-row projection; bind exact recipes, commands, exit codes,
   raw-result hashes and post-restore hashes.
6. Only after 34/34 terminal closure, delete all thirteen inherited Pipeline files and remove the
   empty directory. Update the passive Core requirement projection, profile floor, English build
   documentation, TODO/CHANGELOG as applicable and generated CHANGELIST.
7. Run locked restore and Release build for the focused Core project, the unfiltered focused test
   executable, complete UnitArchitecture, LocalIntegration and Engineering profiles. Required
   outcome is zero failed, skipped or warning tests and zero build warnings/errors. Freeze separate
   technical and evidence commits, then request two independent read-only reviews: product/API and
   test/evidence/34-row closure.

The inherited obligation grouping is fixed:

- `0426`--`0429`: accepted limit carriers;
- `0430`--`0438`: dynamic connection, observer and context-filter carriers;
- `0439`--`0441`: accepted consume-aware retry carriers;
- `0442`--`0447`: configuration plus send/publish layering carriers;
- `0448`--`0450`: accepted partition concurrency plus new convention carriers;
- `0451`--`0454`: accepted retry composition/dispatch carriers;
- `0455`--`0459`: new transaction lifecycle and retry-ownership carriers.

## Quartz scheduling integration execution plan

1. Freeze the accepted Core-pipeline evidence commit and inventory exactly the eighty-seven Quartz
   obligations `OBL-R0-PER-0200..0286`. Keep the separate EF Quartz-outbox obligation outside this
   cohort. Bind one terminal disposition for every included row before deleting the old project.
2. Create the signed source-mirrored executable project
   `tests2/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` using only xUnit 4 and Microsoft
   Testing Platform v2. Add it and the Quartz product project to UnitArchitecture and Engineering;
   do not add the inherited NUnit/VSTest project to any native graph.
3. Update `Quartz` and `Quartz.Extensions.Hosting` together from 3.18.1 to the current stable 3.19.1
   line and restore only the affected modern graph. Locked restores must not renew unrelated old
   test-project lock files.
4. Establish one clock truth. Remove `QuartzTimeAdjustment`; register/inject `TimeProvider` and use
   it for scheduler and outgoing-expiration calculations. Freeze mutable scheduler options before
   runtime use. No process-global delegate, `DateTime.Now`, `DateTime.UtcNow`, sleep or stopwatch is
   permitted in the target product/test paths.
5. Correct the source-derived data-contract defects before accepting their tests: deserialize stored
   header/property JSON through the configured System.Text.Json object deserializer, remove only a
   leading recurring prefix, centralize scheduling keys and validate public arguments and queue
   names. Preserve useful scheduling features; API compatibility with unsuitable inherited shapes
   is not required.
6. Build ordinary xUnit carriers under `Configuration` and `QuartzIntegration` for registration,
   one-shot and recurring trigger construction, exact job data and envelope restoration, cancel,
   pause, resume, replacement, misfire, expiration, job-factory and bus/scheduler lifecycle. Each
   assertion uses an external exact oracle and each asynchronous path has a causal completion
   barrier.
7. Build the retained composition matrix for serializer/header fidelity, courier retry/redelivery,
   missing-saga redelivery, outbox rollback contrast, request/timeout/reschedule, independent saga
   schedules and the complete job-service lifecycle. Reuse existing stronger Core carriers only as
   explicit joint evidence; never count generic Core behavior as Quartz integration without a real
   Quartz boundary.
8. Reread every changed product and test line. Apply the Microsoft test-gap, assertion-quality and
   anti-pattern reviews; then execute bounded one-cause mutations for each product correction and
   every high-risk schedule/control/lifecycle boundary. Restore and hash-check the technical tree
   after each mutation.
9. Only after 87/87 terminal closure and a green focused project remove the inherited Quartz test
   project and its empty directories. Update requirement projection, profile floor, English root
   documentation, TODO/CHANGELOG where applicable and generated CHANGELIST.
10. Run locked restore, Release build and unfiltered focused tests, then complete UnitArchitecture,
    LocalIntegration and Engineering Release profiles. Required outcome is zero failed, skipped or
    warning tests and zero build warnings/errors. Freeze separate technical and evidence commits and
    obtain two independent read-only reviews: product/API and test/evidence/87-row closure.

## Entity Framework Core persistence and outbox execution plan

Work from the accepted Quartz evidence commit `a1195e574e99716b2a10bae390836b1c6c09dcce`
(tree `9362c94e32b30705c2e1566420853714aa5f90d0`). Reconstruct all ninety inherited
obligations `OBL-R0-PER-0004..0092` and `OBL-R0-PER-0119` as one coherent native persistence
cohort. The inherited fixtures are defect history and a semantic lower bound, never the target
test structure.

1. Preserve exactly the current product providers: SQLite for the architecture's NodeLocal and
   Standalone stores, PostgreSQL for the authoritative Master store, and SQL Server including Azure
   SQL as the retained Microsoft relational option. Remove MySQL and Oracle configuration and SQL
   generators only after the retained provider matrix and every affected public call site are
   closed. There is no hidden SQL Server default: configuration must select a provider before the
   runtime graph is materialized.
2. Replace the static type-only lock-statement cache and the executor's first-context snapshot with
   a model-aware provider-owned cache. Its key includes the immutable EF model identity, saga type
   and ordered property set; schema/table/column resolution is therefore never reused across
   incompatible models. Prefer weak model ownership so dynamic models are not retained forever.
   SQLite, PostgreSQL and SQL Server each receive exact positive and hostile SQL-generation tests.
3. Inject `TimeProvider` once through the persistence runtime and replace every product-owned
   `DateTime.UtcNow`, cleanup delay and timed cancellation source in this cohort. Stopwatch use is
   permitted only for elapsed OTel measurement. Tests use deterministic time and causal barriers,
   not sleep, wall-clock windows or absence-until-timeout assertions.
4. Freeze configurator input into immutable runtime settings. Caller-owned delegates, lists, type
   arrays and configurator instances may not mutate saga, inbox, outbox, delivery or cleanup
   behavior after the graph is built. Validate missing context/provider, invalid timing/batch
   values and incompatible concurrency/transaction combinations before runtime start.
5. Keep the deliberate concurrent outbox-add protection introduced upstream to fix duplicate
   `IsDelivered` outcomes, but give synchronization an explicit outbox coordination owner rather
   than locking EF `DbSet` objects. Preserve exact message identity, ordering and single-state-row
   creation under concurrent send/publish and prove cleanup after failure and cancellation.
6. Normalize cancellation and failure ownership throughout saga, inbox and outbox paths. Caller
   cancellation is recognized only when the caller token is actually requested. Infrastructure,
   serialization, provider and observer failures retain their original identity unless the public
   contract explicitly maps them. A duplicate saga-insert race may be recovered only after a
   `DbUpdateException` and a successful repository load prove that the exact saga identity now
   exists. This provider-neutral postcondition is stricter than provider error-code tables; broad
   `catch (Exception)` recovery is forbidden.
7. Build source-mirrored xUnit 4/MTP v2 owners under `Configuration`, `EntityFrameworkCoreIntegration`,
   `Outbox`, `Saga` and `Testing`. UnitArchitecture proves provider-neutral logic, immutable
   configuration and exact SQL. LocalIntegration uses run-scoped real SQLite and PostgreSQL
   databases. SQL Server/Azure SQL remains a visible fail-closed External profile until credentials
   are available; it is never counted green by inventory or skip.
8. Reconstruct the cohort in four larger, internally coherent phases: configuration/provider/model
   contracts; saga repository and concurrency; inbox/bus-outbox delivery and Quartz composition;
   job-service/future persistence and final closure. Reuse stronger accepted native carriers rather
   than duplicating them, while every joint carrier must cross the actual EF boundary it claims.
9. Reread every changed product and test line after each phase. Apply the Microsoft test-gap,
   assertion-quality and anti-pattern rules, then run targeted one-cause mutations for product
   corrections and high-risk transaction, locking, deduplication, rollback, cancellation and
   lifecycle boundaries. Bind exact mutation bytes, commands, results and restore hashes.
10. Delete an inherited EF test file only when every behavior it carried has an equal or stronger
    native owner. Delete resulting empty directories. Final acceptance requires both the 90
    obligation-level contracts and all 156 inherited execution variants to have an explicit,
    mechanically closed disposition. `EXTERNAL_PENDING` is visible work, never terminal green.
    Acceptance also requires locked focused Release builds/tests, unfiltered UnitArchitecture, real
    LocalIntegration, the applicable real External runs, Engineering with zero warnings/errors and
    two independent read-only PASS reviews over separate technical and evidence commits.

Current execution state: the retained provider/configuration/model phase, the saga-concurrency
tranche and the inbox/bus-outbox delivery tranche through the inherited reliable-messaging boundary
are implemented. Native UnitArchitecture proves model-aware SQL caching, explicit provider
selection, frozen saga/outbox configuration, one injected time source, exact insert-race
classification, factory transaction behavior, SQLite optimistic retry/outbox behavior, run-scoped
database naming and explicit outbox write coordination. LocalIntegration uses the canonical
PostgreSQL fixture and proves an actual competing-session `FOR UPDATE` lock, a two-level customized
navigation load and update, bus-outbox commit delivery, scoped send/publish filters, typed database
fault recovery, request-saga fault/trace/scope/delay semantics, EF-to-Quartz commit ordering,
consumer and saga rollback/retry, a real send-pipeline delivery failure and transport-property
round-trip. The inherited product-only `VSB-Fail-Delivery` test switch is removed; delivery failure
is injected at the normal send-observer boundary. The accepted working floors for this still-active
phase are 56 focused EF UnitArchitecture cases, 59 EF LocalIntegration cases, 8 focused Core
outbox-checkpoint and notification cases, 1,888 repository UnitArchitecture cases and 70 repository LocalIntegration
cases, all without skip. The 90 inherited EF obligation contracts have native semantic owners, but
their frozen R0 set expands to 156 execution variants: 48 unparameterized identities, 39 deliberately
provider-neutral identities, 29 real PostgreSQL identities and 40 SQL Server or SQL Server
resiliency identities. The first 116 are executing or provider-neutrally consolidated. The 40 SQL
Server identities remain explicitly `EXTERNAL_PENDING` until the SQL Server and Azure SQL external
cohort runs against real resources; they are not counted green. The inherited EF test project and
its separate verification-model category are retired because its useful behavior is either owned by
the native suite or carried visibly by that external work item, never by a hidden legacy runner.

## Azure Table native persistence execution plan

Work from the clean accepted commit `1d63a7adca71e9e3bac135240d3ff979f7121de1` (tree
`0b024b527475282d378585acd00eea7817a128e7`) under architecture assignment
`PO-2026-08-27-01`. This cohort owns exactly `OBL-R0-PER-0400..0430` and `0450..0457` plus
source-derived gaps in the complete Azure Table product path.

1. Centralize the Table key contract and validate both built-in and caller-supplied saga keys before
   any network call. Reject empty correlation identifiers, empty/oversized keys and every Azure
   forbidden character with precise argument ownership. Reuse the same validator for MessageJournal
   options so there is one storage-key truth.
2. Make entity conversion fail closed. Prove all native Table primitives, nullable omission,
   DateTime/DateTimeOffset UTC semantics, TimeSpan/Uri/Version string encodings, enum/decimal value
   fallback and complex FutureState collections. Malformed persisted values must raise a precise
   conversion error and may never silently become defaults.
3. Normalize saga repository failure ownership: pass caller cancellation through every Table call,
   propagate genuinely requested cancellation unchanged, restrict duplicate insert recovery to an
   actual 409 collision, preserve ETag-based optimistic concurrency and rename/internalize the stale
   Cosmos load context. Add exact argument guards to public factory/configuration entry points.
4. Build source-mirrored UnitArchitecture owners in the existing
   `ViciOne.ServiceBus.Azure.Table.Tests` project for conversion, keys, configuration snapshots and
   cancellation/error classification that needs no resource. Use xUnit 4/MTP v2 only.
5. Factor one run-scoped Azurite table fixture inside the existing LocalIntegration project. Build
   provider-crossing saga owners for insert/load/update/delete, read-only behavior, enum round-trip,
   stale-ETag conflict, bounded retry and caller cancellation. Barriers are causal; no delay,
   inactivity, polling window or shared table is an oracle.
6. Add six stronger Future carriers—completed reuse, fault reuse, fan-in success, fan-in fault,
   nested composition and registration—and one complete job-service lifecycle carrier. Every test
   reads persisted Azure Table state and therefore cannot pass on generic Core behavior alone.
7. Freeze a machine-readable thirty-nine-row R0 disposition. Map `0400..0408` to the accepted
   MessageJournal successor without reconstructing AuditStore; map the saga/job/future and locally
   provable converter/key/repository gaps to exact native test methods. Keep `0455..0457` visibly
   `EXTERNAL_PENDING` for Cosmos-for-Table, Entra ID and real-service limits. Only after all 31
   inherited test obligations and all locally executable source gaps are closed may the old NUnit
   project and resulting empty directories be deleted; External work is never counted green.
8. Update embedded passive requirement projections, solution/CI/documented floors and generated
   CHANGELIST. GitHub workflows remain disabled operationally; their versioned command contracts
   stay internally consistent for later reactivation.
9. Reread every changed product and test line. Apply test-gap, assertion-quality, anti-pattern and
   smell reviews, then run bounded one-cause mutations for every product correction and high-risk
   ETag/cancellation/conversion/lifecycle boundary. Restore and hash-check the technical tree after
   every mutant.
10. Run locked focused restore, Release build, focused UnitArchitecture and Azurite LocalIntegration,
    then complete unfiltered UnitArchitecture, LocalIntegration and Engineering Release. Acceptance
    is zero failures, skips, warnings or errors, separate technical/evidence commits and two
    independent read-only reviews over the exact frozen bytes. Real Azure remains a visible External
    follow-up rather than a local or skipped green claim.

## AWS native transport and persistence execution plan

Work from accepted product commit `427894348e992551c8d2ae15095c416d2cc1b329`, tree
`ae33fd04f6a3957b00ccb4ee59889d91383678e5`, under architecture assignment
`PO-2026-08-27-02`. The immutable R0 input is the 111-row set projected in
`.testagent/aws-native-obligation-map.tsv`. This plan and the research section above must be committed,
pushed and hash-bound by the architecture order before the first product or test edit.

1. Extend the one existing test configuration and fixture pipeline rather than creating AWS-specific
   environment readers. Add `LocalTestResource.LocalStack`, typed host/port/region/account options,
   exact environment projections and independent configuration tests. Add one LocalStack service to
   the canonical Compose file, pinned to the final token-free Community release 4.14.0 and its immutable
   multi-architecture image digest, published
   on loopback with Docker-selected port. Extend runner/log/teardown self-tests before using it.
2. Create six source-mirrored xUnit 4/MTP v2 projects: SQS UnitArchitecture and LocalIntegration,
   DynamoDB UnitArchitecture and LocalIntegration, S3 UnitArchitecture and LocalIntegration. Add
   product projects to matching solution configuration maps so Release tests cannot inspect Debug
   assemblies. No inherited NUnit/VSTest project enters a native graph. Do not activate GitHub runs;
   keep their checked-in command contracts internally consistent for later reactivation.
3. Normalize SQS host composition first. Remove raw access/secret string API, secret-bearing options,
   URI credentials, secret probe fields, `LocalstackHost()` and the shipped `AmazonSqsTestHarness`.
   Retain explicit `AWSCredentials`, injectable service configs/client ownership and AWS SDK v4's
   default provider chain without synchronous configuration-time resolution. Freeze all runtime host
   settings. Prove no public secret surface, no probe secret and exact explicit/default composition.
4. Correct SQS deterministic boundaries before provider tests: one address-construction authority;
   caller names as data, never URI query syntax; exact AWS SQS/SNS grammar and FIFO suffix rules;
   delay, wait, visibility and batch bounds without truncation/overflow; equality/hash consistency;
   immutable batch settings; and fail-fast rejection of receive-queue `RedrivePolicy` while ViciOne
   owns `_error`/`_skipped`. Preserve product error metadata and FIFO grouping semantics.
5. Correct SQS lifecycle and failure ownership. A transient receive/provider failure must back off
   and continue or fault the endpoint explicitly, never silently end a green receiver. Requested
   caller stop is recognized from the requested caller token; dependency cancellation/failure keeps
   its identity. Inject `TimeProvider` into visibility renewal and replace wall-time delay with timer
   ownership. Make purge single-flight, remove the duplicate topology pass and propagate failed
   subscription attribute updates. Every asynchronous test uses a bounded causal barrier and cleanup
   in `finally`.
6. Implement SQS LocalStack carriers in coherent feature groups: connection/configuration and dynamic
   endpoints; exact request/publish/send flows; FIFO/order/filter; error/skipped/redelivery/outbox;
   raw JSON, metadata and OTel; topology/tags/scopes; scheduling and lifecycle. Cross SQS+S3 only in
   the single inherited MessageData transport carrier. LocalStack resource names are derived from the
   run identity plus test identity and are always removed by the owning fixture.
7. Normalize DynamoDB persistence as one saga slice. Remove dead lock options; validate and freeze
   table/config/time input; inject `TimeProvider`; omit null TTL attributes; forward caller tokens;
   propagate requested cancellation unchanged; preserve original provider failures; never return
   null after failed insert; restore the in-memory saga version on failed update; and bind option
   propagation once. LocalStack tests use a unique table and real conditional writes. TTL deletion
   and provisioned-throughput throttling remain explicit External work.
8. Normalize S3 MessageData as one repository slice. Require explicit non-null client and valid
   immutable options at the primary API; fail startup if bucket/lifecycle reconciliation fails;
   create the bucket idempotently; update only the owned lifecycle rule; pass cancellation; validate
   stream, bucket and URN/key boundaries; and preserve exact round-trip bytes. Bind background object
   deletion only to External. If arbitrary per-message TTL cannot receive a complete A+ owner in this
   slice, record it as a release-blocking product TODO rather than silently claiming lifecycle parity.
9. Keep the External matrix visible and fail closed. AWS credential-chain/refresh, real long-running
   receive/visibility behavior, real payload/quota enforcement, partial batch semantics, DynamoDB TTL
   deletion/throttling and S3 lifecycle deletion receive explicit `EXTERNAL_PENDING` rows and a
   source-mirrored External work item. LocalStack evidence never changes those rows to green.
10. After each product group, reread every changed product/test line and the complete associated
    call path. Run focused locked Release restore/build and unfiltered project execution. Then apply
    the mandatory .NET test-gap, assertion-quality, anti-pattern and smell reviews. Correct product
    defects in product code; never weaken expected behavior or test an implementation duplicate.
11. Execute bounded one-cause mutations for each product correction and the high-risk route/error/
    cancellation/ETag/batch/topology boundaries. Bind baseline hash, exact replacement and occurrence,
    mutant hash, fully expanded build/test command, exit code, causal raw output hash and post-restore
    hash. Equivalent mutants are explained and excluded, never counted killed.
12. Delete inherited AWS test files only when all their meaningful rows have equal or stronger native
    carriers and the full 111-row projection is mechanically complete. Delete resulting empty
    directories. Update passive requirements, solutions, floors, docs, TODO, verification ownership,
    generated CHANGELIST and disabled workflow contracts atomically. Final acceptance requires clean
    locked Engineering Release with zero warnings/errors, complete unfiltered UnitArchitecture and
    LocalIntegration with zero failure/skip, focused AWS runs, separate technical/evidence commits,
    two independent read-only PASS reviews and remote backup. External rows stay open until actually
    executed against real AWS.

### Current AWS technical candidate — validation checkpoint

The implementation and locally executable carrier migration are complete but not yet finally
accepted. The 111-row map is mechanically unique and currently resolves to 100
`REPLACED_EXECUTING`, nine `EXTERNAL_PENDING`, one `INVALID_DUPLICATE_RETIRED` and one
`PO_SUPERSEDED_GREENFIELD_API`. The obsolete SQS and DynamoDB projects are removed from the working
tree together with their empty directories. Real AWS remains unexecuted and release-blocking under
`TODO.md`.

The stationary-candidate gates now measured locally are: locked UnitArchitecture and
LocalIntegration restores; both Release solution builds with zero warnings/errors; unfiltered
UnitArchitecture 2032/2032 and LocalIntegration 149/149 with zero failure/skip against one fresh
PostgreSQL/Azurite/LocalStack run; focused SQS 46 Unit and 48 LocalStack, DynamoDB 6 Unit and 9
LocalStack, and S3 6 Unit and 5 LocalStack; plus a complete Engineering locked restore and Release
build with zero warnings/errors. Architecture source/namespace and exact solution-closure guards
pass. Mutation evidence, frozen technical/evidence commits, independent read-only review and remote
backup remain acceptance work; none is inferred from these green executions.

### AWS native correction plan — failure boundaries and deterministic time

The frozen AWS candidate exposed four product defects and one test-oracle defect during the Lead's
mandatory line-by-line audit. Correct them as one bounded follow-up before requesting independent
acceptance; the real-AWS External boundary and the 111-row inherited disposition remain unchanged.

1. Freeze the DynamoDB context factory at registration so retaining and later mutating the
   configurator cannot alter an already registered runtime repository. Cover both public factory
   overloads against a retained-configurator mutation.
2. Make DynamoDB saga loading fail closed when a persisted row contains a JSON null payload or when
   row key, payload correlation id, payload version and row version disagree. Corrupt persistence
   is never reported as an absent or valid saga.
3. Dispose owned SNS and SQS clients independently. A failure from one client may not suppress the
   other's disposal; preserve the exact sole failure and aggregate both failures in deterministic
   SNS/SQS order when both occur.
4. Mark an SQS receive lock lost when the total visibility-renewal window expires. Prove the state
   transition with an injected monotonic sequence and no wall-clock wait.
5. Replace the LocalStack SentTimestamp wall-clock range with a hermetic exact Unix-millisecond
   parser owner. The provider-crossing test retains only provider-origin, UTC and millisecond-shape
   assertions plus the exact envelope timestamp.
6. Raise the predeclared UnitArchitecture floor by exactly eleven materialized cases, from 2032 to
   2043. Run both focused projects, locked UnitArchitecture and LocalIntegration restores/builds,
   complete unfiltered 2043/149 profiles, and the Engineering Release build. Then execute exact
   one-cause mutations in a disposable worktree, freeze separate technical/evidence commits and
   require the same two independent read-only reviews. Do not represent real AWS as locally green.

## ActiveMQ native transport execution plan

Baseline: product commit `9195e5fcde26b5a525d00fe716fcc7c21256f5c1`, tree
`e3310be174e31fa3a999c8dd4d4bc08e535b0ab7`. The exact 113-row inherited projection is
`.testagent/activemq-native-obligation-map.tsv`. Research, this plan and that table must be committed,
hashed and bound by the Lead architecture order before the first product or test edit.

1. Extend the existing typed test configuration with two explicit resources, `ActiveMq` and
   `Artemis`. ActiveMQ carries host, OpenWire, AMQP and Jolokia ports plus run credentials; Artemis
   carries host, its multi-protocol port, Jolokia port and run credentials. Add exact fixture
   environment projections and configuration tests. Missing values fail before discovery. Keep the
   existing pinned Compose images and stable outage proxy; extend only runner self-tests or endpoint
   naming where the current contract is incomplete.
2. Create two source-mirrored xUnit 4/Microsoft Testing Platform v2 owners:
   `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` for hermetic UnitArchitecture and
   `tests2/Transports/ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests` for the real local
   broker paths. Add the product and test projects to exact Release solution mappings. Shared outage
   protocol support belongs in `tests2/Testing/ViciOne.ServiceBus.Tests.Infrastructure/Brokers`, but
   ActiveMQ requirements and assertions remain in the ActiveMQ projects.
3. Normalize host and endpoint construction before provider flows. Remove embedded defaults and URI
   credentials; freeze settings; parse OpenWire/AMQP failover hosts including IPv6; encode every
   option exactly; reject unknown, duplicate, conflicting or invalid query values. Route short,
   full, direct and topology-created addresses through one name-validation/data boundary. Add exact
   positive, negative, roundtrip and control-injection requirements and one-cause mutations.
4. Correct expiration and time ownership. The shared send transport discards non-positive normal
   or generic-forwarding TTL before the transport. Response/Fault remains the sole one-second
   safeguard. ActiveMQ emits exact positive NMS TTL and zero for no expiry. Delayed Artemis send uses
   the context `TimeProvider`. Test every branch with frozen time and recording transport/NMS
   messages, then cross the broker for scheduled delivery without using elapsed silence as an oracle.
5. Remove the false `ConfigureConnection` surface and shipped ActiveMQ test harness. Preserve and
   execute `ConfigureSession`. Make connection and session cleanup attempt every resource exactly
   once and preserve deterministic sole/aggregate failures. Make temporary-entity removal actually
   retire the registry entry and remain retry-safe after deletion failure. Use controlled fakes for
   ownership/order and run-scoped broker residue checks for the provider boundary.
6. Correct topology identity. `ConsumerEntity` equality and hash include topic, queue, selector,
   consumer name and shared mode consistently; its name identity may not merge distinct topic
   subscriptions. Caller topic names remain data. Preserve implemented-message topology only where
   the complete publish-topology traversal actually requires it; do not resurrect the commented
   duplicate traversal without a failing behavior. Prove Classic/OpenWire topic and virtual-topic
   flow, and Artemis/AMQP shared durable load balancing.
7. Implement the 40 UnitArchitecture R0 owners from the frozen table, plus source-derived
   requirements for: secret-free explicit host configuration; strict host/address grammar; topology
   name-as-data; exact TTL/no-expiry/expired discard; context time; no false connection API; complete
   cleanup; temporary registry retirement; complete consumer identity; session-pipe application;
   exact body access-order semantics; recovery-state causality; and compiled requirement projection.
   Every Theory row uses fresh subjects and external expected values.
8. Implement LocalIntegration carriers in coherent broker groups: readiness/admin cleanup/service
   client; OpenWire compression; send/publish/handler/request-response; redelivery/defer/retry;
   serialization and raw-message fault envelopes; outbox exact once; job lifecycle and kill switch;
   exact OTel; topology deployment; cache turnover and outage recovery; temporary replies;
   broker/Quartz scheduling; shared durable subscription; restart/start-stop; topic and virtual-topic
   endpoints. Protocol variants are explicit data rows, never hidden loops.
9. Every asynchronous provider test owns a bounded causal barrier and cleanup in `finally`. There are
   no `Task.Delay`, sleep, stopwatch success windows, fixed names, global mutable recorders, random
   scheduling, broker inactivity or absence-until-timeout oracles. Delay/future cases observe the
   provider's scheduled state plus exact delivery; outage cases require the runner's request/response
   identity and observed-state acknowledgement.
10. Add passive JSON requirement projections to both projects and verify them against the compiled
    xUnit methods. Each selected R0 obligation occurs exactly once in the terminal table; each
    source-derived requirement variant occurs exactly once in compiled metadata. Run the static
    assertion, anti-pattern, gap and smell reviews after each group and correct product defects in
    product code rather than weakening tests.
11. Execute focused locked Release restore/build and unfiltered project runs after every coherent
    group, then the complete UnitArchitecture and LocalIntegration profiles and Engineering Release
    build with zero failures, skips, warnings or errors. Broker-backed runs go only through
    `run_broker_category.py` with ActiveMQ and Artemis explicitly selected. On this host, retain the
    established isolated `DOTNET_CLI_HOME`, disabled build servers/shared compilation and approved
    outside-sandbox execution; a socket denial or sleeping orphaned worker is an environment
    diagnosis, never permission to edit tests.
12. For every product correction and high-risk route/cancellation/cleanup/topology boundary, bind an
    exact one-cause mutation: baseline hash, occurrence/index, replacement or patch, mutant hash,
    fully expanded command, exit code, causal raw output hash and post-restore hash. Equivalent
    mutants are named and excluded. Positive evidence binds CTRF, broker wrapper/logs, locked
    restore/build binlogs and complete profile summaries.
13. Delete the inherited ActiveMQ test project only after all 113 rows have terminal executing
    carriers and all complete product paths are green. Delete every resulting empty directory and
    remove obsolete NUnit/VSTest/Ionic test-only dependencies and verification-model entries. Update
    solutions, floors, docs, TODO, generated CHANGELIST and the still-disabled workflow command
    contracts atomically; do not reactivate GitHub Actions in this slice.
14. Freeze separate technical and direct evidence commits on the product branch and push both. Final
    acceptance requires two independent strict read-only reviews of the same commit/tree covering
    every changed product/test line, structure, 113-row closure, source-derived requirements,
    mutations and Evidence. Team 1 remains review-only and starts only on an explicit frozen scope.

## Transactional bus capability split execution plan

Work from accepted commit `b198d26bc311dd8946006534ebddd8d5282e8006`, tree
`c25e20f35cf493e906cd7688f6320d91f09004d3`. Before the first product or test edit, commit this plan,
the research section above and `.testagent/transactional-bus-obligation-map.tsv`, push the checkpoint
and bind their hashes plus the exact scope in the Lead architecture order.

1. Replace `ITransactionalBus` with two truthful Greenfield capabilities:
   `IAmbientTransactionBus : IBus` and `IBufferedBus : IBus` with
   `Task FlushAsync(CancellationToken)`. Remove the intentionally throwing ambient `Release` surface;
   backward API compatibility is not required.
2. Rename/internalize the implementations around their behavior. The ambient adapter follows
   `Transaction.Current`; the explicit adapter is an internal sealed FIFO buffer. Keep shared
   send/publish forwarding only where both capabilities have identical semantics. Reject null
   collaborators at construction/registration.
3. Make explicit buffering deterministic: FIFO snapshot, one serialized drainer, empty/repeated
   flush no-op, exact dispatch failure/cancellation identity, no replay of an already attempted
   action, and retention of only not-yet-attempted work. Caller cancellation belongs to enqueue only
   until enqueue completes; flush cancellation belongs to dispatch. Prove additions during an active
   drain are held for the next flush.
4. Make ambient ownership explicit: immediate dispatch without `Transaction.Current`; FIFO prepare
   once within a transaction; exact commit, rollback and in-doubt cleanup; independent concurrent
   transaction state; and transaction abort with the original transport failure retained as cause.
   Document that this volatile prepare bridge is best-effort, not a durable atomic outbox.
5. Replace the two ambiguous registration families with `AddAmbientTransactionBus` and
   `AddBufferedBus`, including generic multi-bus forms. Use required-service resolution and distinct
   scoped context providers. Prove the public interfaces, implementation non-publicity, lifetimes,
   generic bus binding and absence of the retired public names/method-only-throw surface.
6. Add source-mirrored xUnit 4/MTP v2 owners under
   `tests2/ViciOne.ServiceBus.Tests/Transactions`. Reuse bounded internal-access recording drivers;
   do not reference the inherited TestFramework. Cover the eleven inherited obligations plus API,
   FIFO, empty/repeated flush, concurrency, cancellation, failure state and transaction isolation.
7. Add unique passive Core requirement variants and compile them against exact xUnit methods. Freeze
   the eleven inherited rows as `REPLACED_EXECUTING` in the committed map; consolidation is allowed
   only where one stronger method independently proves both pre-release absence and post-release
   exact execution.
8. Reread every changed product/test line and the complete transaction, DI, scoped-context and
   EF-integration call paths. Run mandatory .NET assertion-quality, anti-pattern, gap and smell
   reviews. Product defects are fixed in product code; no native test encodes an inherited defect or
   timeout-based absence.
9. Execute focused locked Release restore/build and transaction tests, then complete unfiltered
   UnitArchitecture, LocalIntegration and Engineering Release with zero failure, skip, warning or
   error. Raise every active floor from the materialized test count and keep GitHub workflows
   operationally manual/disabled.
10. Bind byte-exact one-cause mutations for API split, wrong DI contract, null fallback, FIFO,
    pre-flush execution, duplicate execution, swallowed failure/cancellation, rollback delivery and
    transaction-state leakage. Record argv/cwd, raw output, exit code, mutant and post-restore hash.
11. Delete exactly the three inherited transaction test files after terminal closure; delete the
    directory only if empty. Do not claim or retire the rest of `tests/ViciOne.ServiceBus.Tests`.
    Update requirements, solutions/floors/docs/TODO and generated CHANGELIST atomically.
12. Freeze separate technical and evidence commits, require two independent strict read-only PASS
    reviews over the same bytes, then fast-forward both product and architecture branches to their
    remotes. The later full Core closure and atomic `tests2` promotion remain separate slices.
## AWS native closure — bounded review-correction plan

This plan is authorized by architecture commit `c7a8b28` and is additive to the frozen AWS-native
plan. It changes neither the 111-row R0 disposition nor the real-AWS External boundary.

1. Replace the untyped DynamoDB context delegate with an internal immutable
   `DynamoDbContextFactory<TSaga>` owner. Make every repository factory consume its saga-specific
   owner and prove two distinct saga registrations in both orders with exact context identity.
2. Replace arbitrary public SQS host-setting implementations with one sealed immutable snapshot.
   Keep mutation inside the configurator only, freeze the connection factory and cache options, and
   reject user info, query and fragment through the single `AmazonSqsHostAddress` input boundary.
3. Introduce immutable validated per-host SQS cache options carrying capacity, maximum age and
   `TimeProvider`. Pass them explicitly through the host snapshot into queue/topic caches; delete
   process-global defaults and the unused minimum-age API. Prove two-host isolation, builder freeze,
   invalid boundaries and deterministic expiration without sleeps or wall-clock assertions.
4. Route both SQS/SNS constructors through one atomic pair builder. On second-client failure always
   clean the first client; preserve the primary instance and stack if cleanup succeeds, otherwise
   throw a deterministic `AggregateException(primary, cleanup)`. Strengthen normal disposal with
   stack-provenance assertions.
5. Mark SQS receive-lock ownership lost after every attempted `Complete`, including provider failure
   and requested cancellation, while preserving the exact original exception. Prove subsequent
   `ValidateLockStatus` fails closed.
6. Update the passive AWS requirement projection with unique variants for each new carrier. Use
   xUnit 4/MTP v2, source-mirrored namespaces, fresh subjects, exact identities and bounded causal
   barriers. The twelve materialized cases raise the UnitArchitecture floor from 2043 to 2055;
   LocalIntegration remains 149 because these are hermetic configuration/ownership defects.
7. Run focused locked Release builds and tests, then full UnitArchitecture, LocalIntegration and
   Engineering profiles with isolated CLI home, build-server/node reuse disabled and no shared
   compilation. A restricted-sandbox NamedPipe failure is rerun unchanged outside the sandbox; tests
   are never weakened for the environment.
8. Bind one-cause mutations for typed Dynamo ownership, host snapshot/URI rejection, per-host cache
   propagation, client-pair primary/cleanup handling, Complete lock loss and EDI stack preservation.
   Record commands, CTRF/raw results, hashes and byte-identical restoration.
9. Freeze separate technical and direct evidence commits, push the correction branch, and require
   both independent reviewers to return PASS against the exact hashes before architecture acceptance.
   GitHub Actions remain disabled and real AWS remains untouched.

## AWS native closure — final host-input correction

The second independent Evidence review found one remaining public input gap: an absolute
`amazonsqs:/scope` URI has no host but passed the central address boundary. Close that gap together
with explicit carriers for the already intended relative-URI and string-host guards. The two new
theories materialize five hermetic cases, raising the UnitArchitecture floor from 2055 to 2060 while
LocalIntegration remains 149. Bind three separate one-cause mutations for hostless URI, relative URI
and null/empty/whitespace string host. Regenerate the Evidence child and make M32's replacement
literal encompass its unique `Complete` finally block rather than claiming the repeated exchange
line is unique.
## F-REP-02 — identity/provenance reconciliation implementation

1. Replace the binary existing/missing baseline contract with an exact 5,654-key terminal partition:
   `MAPPED_EXISTING`, `MOVED_EXACT` or `RETIRED_DELETED`. Bind retirement to baseline blob/mode,
   generated CHANGELIST deletion and the Git deletion commit; reject missing, duplicate, invented,
   resurrected and falsely retired records.
2. Treat ordinary content changes as recorded modernization rather than a failed identity-only
   refactor. Preserve the independent anti-sabotage check for mapped legacy tests. Public declaration
   records receive explicit present/modified-or-removed/retired-path dispositions instead of
   inventing Phantom targets.
3. Add a schema-versioned historical-identity policy whose entries bind one exact current path and
   category, with exact counted line digests for UTF-8 content and a complete blob SHA-256 for binary
   content. No directory/extension wildcard is supported. Missing/stale policy entries and any
   additional former identity remain findings; unrelated text additions do not stale an authority.
4. Extend persisted-evidence validation to the terminal mapping and historical policy contract; add
   a deterministic manifest for generated data contracts. Regenerate the Identity Evidence only in
   the later direct Evidence child.
5. Add independent hostile tests for terminal partition completeness, resurrection, missing deletion
   provenance, false retirement, wrong move/target binding, stale/extra historical policy, product
   leakage, public-declaration dispositions and persisted Missing/Extra/Duplicate/drift.
6. Run all Identity self-tests, then the complete full-tree scan. Completion requires Exit 0, zero
   findings and exactly 5,654 unique terminal baseline keys; a green focused suite alone is not a
   verdict.
7. Verify product/test/solution/workflow/floor bytes against `1c52dd2d`, freeze a tooling-only Technical
   commit, generate a direct Evidence/status/CHANGELIST child, and require two independent read-only
   PASS reviews before closing F-REP-02.

Acceptance checklist to concrete test owners:

- Missing/duplicate/invented terminal keys → `IdentityGateTerminalDispositionTests`.
- Deleted target without CHANGELIST/Git provenance and resurrected retired target →
  `IdentityGateTerminalDispositionTests`.
- Wrong moved source/target/blob → existing exact format-binding owner plus new terminal-binding cases.
- Historical-policy stale context/blob digest, missing entry, extra entry and active-product copy →
  `IdentityGateHistoricalContextTests`.
- Retired/modified public declarations and persisted record drift →
  `IdentityGatePublicProjectionTests` and existing `validate_evidence_records` owners.
- Canonical 5,654-key partition and full scan zero findings → final read-only integration carriers,
  not fixture-only assertions.

## Diagnostics native closure execution plan

1. Bind `OBL-R0-SML-0222..0265` in `.testagent/diagnostics-native-obligation-map.tsv` to 44 unique
   source-mirrored xUnit carriers: 11 command-line, 10 ledger, 11 observation-boundary and 12
   result-delivery/cancellation cases. The five inherited verdict Theory rows remain five separately
   attributable executable cases.
2. Add an optional internal `TimeProvider` to `MessageSequenceLedger.WaitForAllExpected`,
   `PublishLoadScenario.Quiesce` and `ObserveThenQuiesceThenRead`, defaulting to
   `TimeProvider.System`. Preserve production behavior while making timeout, cancellation and window
   completion causally advanceable.
3. Create `tests2/Tools/ViciOne.ServiceBus.Diagnostics.Tests` as an xUnit 4/MTP v2 executable test
   project with the existing friend-assembly identity, `FakeTimeProvider` and the common requirement
   verifier. Each test owns fresh state and external expected values; temporary files are unique and
   removed in `finally`/`Dispose`.
4. Embed an exact passive JSON projection for all 44 product requirements plus its projection Fact.
   Add the test and product projects to UnitArchitecture, replace the inherited project in
   Engineering, and raise every active Unit floor from 2,288 to 2,333. GitHub workflows remain
   operationally disabled; only their manual command contract is updated.
5. Run focused locked Release restore/build and unfiltered native Diagnostics tests, then complete
   UnitArchitecture, LocalIntegration and Engineering Release verification. Use the established
   isolated CLI home/build-server settings; a restricted-sandbox pipe/socket failure is rerun
   unchanged outside the sandbox and never fixed by weakening tests.
6. Perform the mandatory assertion-quality, anti-pattern, test-gap and smell reviews. Bind targeted
   one-cause mutations for the injected clock, quiescence-result boundary, snapshot ordering,
   exactness and result-delivery fallbacks.
7. Once all 44 inherited rows are terminal `REPLACED_EXECUTING`, delete the three inherited C# files,
   old NUnit/VSTest csproj and lock file, including the now-empty directory. Regenerate CHANGELIST,
   freeze technical and direct evidence commits, require independent read-only review, then request
   remote push approval for the new commits.

## SQL transport native closure execution plan

1. Materialize an exact 135-row SQL obligation map from the frozen ledger and keep the 110 inherited
   identity census auditable. Split execution by actual environment: 44 hermetic obligations in
   `ViciOne.ServiceBus.SqlTransport.Tests`, 55 PostgreSQL rows and 36 SQL Server rows in separate
   LocalIntegration projects. The obsolete cross-provider runtime dialect resolver is retired
   explicitly because the two native provider executables no longer dispatch through that helper;
   the other 43 hermetic obligations remain executing Facts.
2. Rebuild the hermetic address, runner-contract, channel-name, connection-string, multi-host,
   instance and port contracts first. Use xUnit 4/MTP v2, source-mirrored namespaces, passive
   requirement JSON and independent expected values; do not copy NUnit fixture inheritance.
3. Build one run-scoped provider harness per engine from the pinned compose contract. Database
   create/drop, connection identity, schema and transport operations must use positive provider
   acknowledgements and bounded operation tokens, never sleeps or absence-only timing oracles.
4. Rebuild every PostgreSQL and SQL Server behavior row against its owning product path, including
   provisioning, topology, faults, outbox, scheduling, purge, jobs, renew-lock, delayed delivery,
   serialization, delivery limits, partitioning, publish/request and redelivery-header behavior.
5. Add all three native projects to the exact Unit/Local/Engineering solution graphs, bind their
   requirement projections, update the active floors once from actual discovery and extend the
   fixture runner only where the existing pinned PostgreSQL/SQL Server contract requires it.
6. Perform assertion-quality and pseudo-mutation review, then run focused projects, complete
   UnitArchitecture, complete LocalIntegration and Engineering Release once. Mutations must target
   provider selection, run-scoped identity, address parsing, schema/provisioning and at least one
   end-to-end delivery invariant.
7. Only after all 135 obligations are terminal (134 executing and the one obsolete runtime resolver
   explicitly retired) delete the complete inherited SQL project (37 C# files,
   csproj, compose and lock file), remove the empty directory and legacy verification category,
   regenerate CHANGELIST and freeze Technical plus direct Evidence children. If one provider is
   genuinely blocked, restore a coherent non-retired phase and continue another planned task rather
   than leaving an ambiguous half-deletion.

SQL closure checkpoint: 43 hermetic R0 Facts plus one requirement-projection Fact execute 44/44;
PostgreSQL executes 71/71 and SQL Server executes 60/60. The exact 135-row terminal map binds 134
executing obligations and explicitly retires only OBL-R0-SQL-0054, the obsolete cross-provider
runtime dialect resolver. The complete inherited 40-file SQL project and its empty directory are
removed. UnitArchitecture executes 2,399/2,399; the independent accepted floor is 2,394. The
LocalIntegration profile adds 131 SQL cases to its prior 244 and therefore carries floor 375.

## Core transform native closure execution plan

1. Bind `OBL-R0-CORE-C-0441..0449` one-to-one to nine source-mirrored xUnit 4/MTP v2 Facts in
   `ViciOne.ServiceBus.Tests.Transformation.TransformPipelineTests`.
2. Preserve and strengthen the inherited behavior boundaries: send and publish transforms remain
   isolated, endpoint transforms distinguish in-place replacement from new-message creation,
   handler-specific interface transforms do not alter an independently ready control endpoint, and
   class-based specifications apply every configured property.
3. Use only positive transport/endpoint-ready and message-consumed barriers with the run-scoped
   operation timeout. No sleep, wall-clock delay or absence-only completion owns a verdict.
4. Raise the independent UnitArchitecture floor by exactly nine materialized cases, from 2,394 to
   2,403. LocalIntegration remains 375 because the in-memory transform pipeline is hermetic.
5. Delete the three inherited Transform C# files only after all nine native cases and the passive
   requirement projection are green, then remove the now-empty `tests/.../Transforms` directory.
6. Run the focused project, complete UnitArchitecture and Engineering Release checks, perform
   one-cause mutation checks for send/publish isolation, Replace identity and class-specification
   application, then freeze separate Technical and Evidence commits. Remote publication remains a
   separately authorized action.

## Message-fabric and topic-routing native closure execution plan

1. Bind `OBL-R0-CORE-C-0450..0454` one-to-one to five source-mirrored xUnit 4/MTP v2 Facts in
   `ViciOne.ServiceBus.Tests.Transports.Fabric`.
2. Prove the complete acyclic exchange-to-exchange and exchange-to-queue graph by exact object
   identity, and prove that a rejected cycle leaves every previously valid edge intact and adds no
   partial destination edge.
3. Exercise topic routing directly through the product exchange with deterministic in-process
   delivery contexts. Bind `#`, `car.*` and `car.*.large` independently and assert the exact routed
   message set without sleeps, wall-clock windows, brokers or absence-only completion oracles.
4. Raise the independent UnitArchitecture floor by exactly five materialized cases, from 2,403 to
   2,408. LocalIntegration remains 375 because the fabric and topic node are hermetic.
5. Delete the two inherited Transports C# files only after the five native cases and passive
   requirement projection are green, then remove the now-empty `tests/.../Transports` directory.
6. Run focused, complete UnitArchitecture and Engineering Release checks, then perform one-cause
   mutation checks for cycle validation, hash routing and both wildcard branches. Freeze separate
   Technical and Evidence commits; remote publication remains separately authorized.

## Core send-topology native closure execution plan

1. Bind `OBL-R0-CORE-C-0428..0429` one-to-one to two source-mirrored xUnit 4/MTP v2 Facts in
   `ViciOne.ServiceBus.Tests.Topology.Configuration.TopologyConventionIntegrationTests`.
2. Prove all three inherited correlation-selector surfaces in one coherent positive transport flow:
   an interface-specific bus topology selector, the built-in `CorrelationId` property convention and
   the public global message selector. Assert both envelope and message values independently.
3. Prove that a message-specific send-topology serializer selects raw System.Text.Json across a real
   in-memory publish/consume boundary, including the exact received content type and payload.
4. Raise the independent UnitArchitecture floor by exactly two materialized cases, from 2,408 to
   2,410. LocalIntegration remains 375 because both topology flows are hermetic.
5. Delete the two inherited Topology C# files only after both native cases and the passive requirement
   projection are green, then remove the now-empty `tests/.../Topology` directory.
6. Run focused, complete UnitArchitecture and Engineering Release checks, then bind one-cause
   mutations for correlation-selector application and serializer selection. Freeze separate Technical
   and Evidence commits; remote publication remains separately authorized.

## Consumer-factory middleware retirement execution plan

1. Bind `OBL-R0-CORE-D-0032` to the already executing xUnit 4/MTP v2 ConsumerFactory theory case in
   `ConsumerMessageConfigurationTests`; do not add a redundant Fact or raise the test floor.
2. Require the stronger native carrier to prove the consumer-level pipe executes exactly once with
   the exact consumer instance and message, alongside the message and consumer-message layers.
3. Delete `tests/ViciOne.ServiceBus.Tests/ConsumerFactoryMiddleware_Specs.cs` only after the focused
   ConsumerFactory case and passive requirement projection remain green. The legacy project root is
   retained because it contains unrelated inherited files; remove no unrelated path.
4. Run the focused carrier, complete UnitArchitecture and Engineering Release checks, and bind one
   exact product mutation that omits consumer-level pipe-specification registration. Freeze separate
   Technical and Evidence commits. Remote publication remains separately authorized.

## Consumer request/response retirement execution plan

1. Bind `OBL-R0-CORE-D-0033` to the already executing xUnit 4/MTP v2 DI harness Fact
   `DependencyInjectionTestHarnessTests.ScopedRequestClient_RecordsTheExactRequestAndResponse`;
   do not add a redundant Fact or raise the test floor.
2. Require the stronger native carrier to prove the registered class consumer receives the exact
   correlation id, returns the exact response, preserves the request id and records both terminal
   observations without consume or send exceptions.
3. Delete `tests/ViciOne.ServiceBus.Tests/Consumer_Specs.cs` only after the focused carrier and
   passive requirement projection remain green. Retain the legacy project root and every unrelated
   inherited test.
4. Run the focused carrier, complete UnitArchitecture and Engineering Release checks, and bind one
   exact product mutation that omits scoped consumer attachment at the DI receive endpoint. Freeze
   separate Technical and Evidence commits. Remote publication remains separately authorized.

## In-memory dual-host isolation native closure execution plan

1. Bind `OBL-R0-CORE-D-0175` one-to-one to a source-mirrored xUnit 4/MTP v2 Fact in
   `ViciOne.ServiceBus.Tests.InMemoryTransport.InMemoryTransportIsolationTests`.
2. Start two run-scoped in-memory virtual hosts, connect them only through the inherited explicit
   relay, and prove the external relay forwards exactly once while the internal relay suppresses the
   return path exactly once.
3. Bind the original message id and source address across both fabrics, then stop/drain both buses
   before asserting exact relay and real-consumer invocation counts. Use only positive observations
   and the configured operation timeout; no sleep or absence-only completion owns the verdict.
4. Raise the independent UnitArchitecture floor by exactly one materialized case, from 2,410 to
   2,411. LocalIntegration remains 375 because both transports are hermetic.
5. Delete `tests/ViciOne.ServiceBus.Tests/InMemoryDuo_Specs.cs` only with the executing native case
   and its passive requirement projection in place. Retain every unrelated inherited file and remove
   any directory only if the deletion makes it empty.
6. Run the focused carrier, requirement projection, complete UnitArchitecture and Engineering
   Release checks. Bind separate one-cause mutations for virtual-host separation and source-address
   preservation, restore every product byte exactly, then freeze Technical and direct Evidence
   commits. Remote publication remains separately authorized.

## In-memory outbox direct-path and lifecycle native closure execution plan

1. Bind `OBL-R0-CORE-D-0176..0181` one-to-one to six source-mirrored xUnit 4/MTP v2 Facts in
   `ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox.InMemoryOutboxFilterTests`.
2. Preserve the independent payload control, then drive the real filter with a foreign empty service
   scope and no bus-bound setter. The downstream pipe must receive the real outbox context without
   resolving or replacing anything from the foreign provider.
3. Prove success ordering by recording consume completion before the pending action. On failure,
   retain the exact original exception and manually drain the captured outbox after the filter
   returns to prove that the action was actually discarded rather than merely left unexecuted.
4. Prove exact scope replacement/restoration order independently for success and failure, including
   the same scope and the exact real outbox context received by the setter. Use only immediate
   in-process state transitions; no sleep, wall clock or absence-only terminal verdict.
5. Raise the independent UnitArchitecture floor by exactly six materialized cases, from 2,411 to
   2,417. LocalIntegration remains 375 because the complete filter boundary is hermetic.
6. Delete `InMemoryOutboxDirectPath_Specs.cs` and `InMemoryOutboxLifecycle_Specs.cs` only after all
   six mappings and the passive requirement projection are valid. Retain every unrelated inherited
   file and remove a directory only if it is empty.
7. Run the focused six-case carrier, projection, complete UnitArchitecture and Engineering Release
   checks. Bind one-cause mutations for the null-setter guard, pending-action execution, failure
   discard and scope restoration; restore every product byte exactly, then freeze Technical,
   Evidence and architecture children. Remote publication remains separately authorized.

## Core package 0182-0199 native closure execution plan

1. Close `OBL-R0-CORE-D-0182..0199` as one 18-obligation package. Reuse the stronger executing
   handler-harness, buffered-object-send, dynamic-interface-contract and structured-probe carriers;
   do not duplicate their behavior merely to preserve inherited fixture names.
2. Add exactly three virtual-time outbox/redelivery cases: message-scoped publish, message-scoped
   send and endpoint-scoped publish. Each must traverse immediate retry plus delayed redelivery,
   reach one terminal `Fault<T>`, record the exact redelivery sequence and prove that no failed
   attempt flushes its deferred send or publish.
3. Add one around-consumer factory-filter case that proves the exact before/consumer/after sequence,
   consumer identity and message identity. Add one interface-dispatch case that sends a concrete
   two-interface message and, after terminal bus drain, proves exactly one delivery with exact values
   to each interested interface handler.
4. Raise the independent UnitArchitecture floor by exactly five materialized cases, from 2,417 to
   2,422. LocalIntegration remains 375 because every new carrier is hermetic.
5. Delete `InMemoryOutboxRedelivery_Specs.cs`, `InMemoryTest_Specs.cs`,
   `InterceptingConsumer_Specs.cs` and `InterfaceSubscription_Specs.cs` only with all eighteen
   terminal map rows present. `OBL-R0-CORE-D-0199` remains bound to its already accepted structured
   probe and creates no new case.
6. Run focused tests while implementing, then execute complete UnitArchitecture, Engineering,
   tool-gate, Identity and Evidence closure once for the whole package. Bind independent mutations
   for the redelivery/outbox integration, consumer-filter return path and multi-interface dispatch;
   restore every product byte exactly before the single Technical freeze. Remote publication remains
   separately authorized.

## Core job-service package 0201-0227 native closure execution plan

1. Close `OBL-R0-CORE-D-0201..0227` as one twenty-seven-obligation UnitArchitecture package; keep
   the already closed `0200` configuration obligation in its existing C26 owner.
2. Add a source-mirrored state-machine Theory for all eleven job-saga states plus an independent
   current-attempt control. Each row raises started, completed, faulted, canceled and start-faulted
   stale events and asserts the complete saga snapshot remains byte-for-byte equivalent.
3. Add hermetic in-memory job-service carriers for permanent fault, configured retry, cancellation
   with terminal status, retry after cancellation, slot-wait cancellation, exact completion,
   generated identity, unknown identity and an actually observed scoped publish filter. Use only
   positive transport/message barriers and the configured operation timeout.
4. Read the built bus probe for both container-less and registration-context endpoint paths and bind
   the outbox on Job, JobAttempt and JobType independently; retain the explicit no-scope assertion
   only on the container-less path where it is causal.
5. Drive `JobService` directly through publication gates. Prove both lifecycle-overlap directions,
   in-flight heartbeat draining, replacement of repeated heartbeat generations, failure recovery,
   the complete admission state sequence and the admitted-but-not-registered stop boundary without
   `Task.Delay`, `Thread.Sleep` or a quiet-window verdict.
6. Add the twenty-eight materialized native cases to the UnitArchitecture floor, from 2,422 to 2,450, and map all twenty-seven
   obligations to exact test methods. Delete the five inherited files only after focused execution
   and requirement projection are green; remove directories only when actually empty.
7. Run one final locked restore, Engineering Release build, full UnitArchitecture profile, format,
   CI-tool, identity and CHANGELIST closure. Bind independent one-cause mutations, restore every
   product byte exactly, then freeze Technical, direct Evidence, status and architecture children.
   Remote publication remains separately authorized and never uses force-push.

## Core runtime scheduling, redelivery and lifecycle package execution plan

1. Close the exact thirty-four-obligation set `OBL-R0-CORE-D-0111..0114`, `0335..0345`,
   `0426..0432` and `0453..0464` as one UnitArchitecture package. Do not absorb unrelated gaps merely
   because their numeric identifiers are adjacent.
2. Add virtual-time delayed-redelivery owners for the exact interval sequence, message-id replacement,
   exception-specific filter coexistence and outbound header isolation. Every scheduled transition
   must be observed before advancing `IInMemoryDelayProvider`; no wall-clock wait owns a verdict.
3. Add deterministic recurring-job carriers for cancel/re-add/manual continuation, distinct and stable
   named identities across changed and unchanged schedules, and provider-timed one-shot execution.
   Retain four independent cron-expression data rows.
4. Add one complete carrier each for service-instance endpoint topology, built pipeline composition,
   start/stop/restart request routing and message activity propagation. Bind all inherited assertions
   after a positive product barrier and consolidate only where one flow observes the entire invariant.
5. Strengthen `TestHarnessTimeProviderTests` with current-scope cancellation isolation, explicit-cancel
   renewal and harness-lifetime inactivity after budget expiry. Reuse its existing expired-budget,
   rolling-deadline and idempotent-dispose Facts for the remaining inherited identities.
6. Raise the independent UnitArchitecture floor by exactly eighteen materialized cases, from 2,450 to
   2,468. Add a machine-readable 34-row terminal map and passive Core requirement rows, then delete all
   nine fully replaced inherited files and remove directories only if they are actually empty.
7. Execute focused tests while implementing, then one locked Engineering build, complete
   UnitArchitecture profile, CI tools, Identity and CHANGELIST closure for the whole package. Bind
   independent one-cause mutations, restore every product byte, and freeze Technical, direct Evidence,
   status and architecture children. Remote publication remains separately authorized and never uses
   force-push.

## Core fault and publication package execution plan

1. Close exactly `OBL-R0-CORE-D-0144..0167`, `0306..0310` and `0318..0324` as one thirty-six-
   obligation UnitArchitecture package. Reuse the eleven stronger native owners and do not create
   renamed duplicates.
2. Add one source-mirrored in-memory error-transport carrier that binds the move plus correlation,
   source, destination, response and fault addresses after a positive error-endpoint delivery.
3. Add one exact four-event consumer flow, one 500-message/32-concurrency fault-storm carrier and one
   `PublishFaults=false` carrier whose moved-error terminal barrier makes the zero-fault assertion
   deterministic.
4. Add one five-row polymorphic-fault Theory and one seven-operation publish-overload matrix. Each
   operation must carry an independent identity/metadata oracle and all final counts are asserted only
   after transport stop/drain.
5. Map all thirty-six obligations to exact executing methods, raise the UnitArchitecture floor only by
   the actual new cases, then delete `ErrorQueue_Specs.cs`, `EventPublish_Specs.cs`,
   `ExcessiveAsyncFault_Specs.cs`, `FaultPublish_Specs.cs`, `PolymorphicFault_Specs.cs` and
   `PublishSubscribe_Specs.cs` atomically. Remove directories only when they are empty.
6. Iterate with focused Release build/test commands only. Once green, run one locked Engineering
   build, one complete UnitArchitecture profile, scoped format, CI tools, Identity and CHANGELIST.
7. Bind a small independent mutation set for the real product seams, restore every source byte, then
   freeze one Technical commit, one direct Evidence child and one architecture binding. Remote push
   remains separately authorized and never uses force-push.

## Core batch, mediator and in-memory messaging 56-obligation execution plan

1. Close `OBL-R0-CORE-B-0025..0039` together with Core-D `0014..0020`, `0025..0026`,
   `0141..0143`, `0233..0241`, `0278..0280`, `0325..0326`, `0395..0402` and `0416..0422` as one
   fifty-six-obligation UnitArchitecture package.
2. Reuse and, only where required, strengthen the existing interface-dispatch, mediator-request,
   multi-consumer and dynamic-endpoint owners. Add no test whose only purpose is to preserve an old
   fixture or method name.
3. Add compact source-mirrored owners for batch completion/grouping/deduplication/fault/outbox/
   overlap, mediator dispatch and cancellation, consumer/saga concurrency, endpoint-convention
   routing, consume-context payload propagation and the seven send overloads. Each owner uses a
   positive product barrier and an exact terminal count or identity oracle.
4. Create a machine-readable 56-row obligation map and passive requirement bindings. Raise the floor
   only by the actual materialized xUnit cases.
5. Delete `Batch_Specs.cs`, `ConcurrencyLimit_Specs.cs`, `Enrichment_Specs.cs`, `Mediator_Specs.cs`,
   `MultiTestConsumer_Specs.cs`, `ReceiveEndpoint_Specs.cs`, `SendByConvention_Specs.cs`,
   `SendContextMiddleware_Specs.cs`, `SendReceive_Specs.cs` and `ContainerTests/Batch_Specs.cs`
   atomically after all carriers are green; remove only directories that are then empty.
6. Iterate using focused Release build/test only. Then run exactly one locked restore, one clean
   Release build, one complete UnitArchitecture profile, scoped format and repository gates.
7. Bind one compact independent mutation block across the actual batching/mediator/routing seams,
   restore every product byte, then freeze one Technical commit, one direct Evidence child and one
   architecture binding. No remote push occurs without a new explicit authorization.

## Core serialization 58-obligation execution plan

1. Close exactly `OBL-R0-CORE-C-0153..0166`, `0172..0179`, `0180..0185`, `0275..0294`,
   `0343..0351` and `0464` as one UnitArchitecture package.
2. Reuse the accepted MessagePack formatter-cache, scalar, envelope-clone, hardening, interface,
   job, MessageData, ReceiveFault and redelivery carriers plus the exact envelope-metadata owner.
   Add no renamed duplicate for behavior those tests already prove more strongly.
3. Add compact System.Text.Json owners for the remaining challenging shapes, interface dispatch,
   job dictionary, MessageData reference, ReceiveFault variants, raw virtual-time redelivery and raw
   concrete-to-interface dispatch. Add one MessagePack integration owner for JSON request plus
   MessagePack response with both exact content types.
4. Use positive product-owned completion or scheduled-redelivery barriers and stop/drain before
   terminal exact-count assertions. No `Task.Delay`, sleep or absence-only success oracle is allowed.
5. Add one 58-row terminal map and eight passive requirement bindings. Raise the executable floor by
   the nine materialized cases from 2,514 to 2,523.
6. Delete all eleven fully replaced inherited Serialization files atomically after focused green
   execution. Verify that their directory is then absent; do not touch the historical expected-core
   identity list.
7. Run one locked restore, one Engineering Release build, one complete UnitArchitecture profile,
   scoped format and repository gates. Bind a compact independent mutation set across the actual
   serializer/transport seams, restore every byte, then freeze one Technical commit, one direct
   Evidence child and one architecture binding. Remote publication remains separately authorized.

## Core Courier native closure — large-package execution plan (2026-08-30)

1. Freeze exactly `OBL-R0-CORE-C-0018..0098` in one TSV map; reject missing, duplicate, unknown or
   non-`REPLACED_EXECUTING` rows.
2. Add source-mirrored Courier xUnit carriers, consolidated by observable lifecycle rather than by
   old fixture class:
   - `RoutingSlipBuilderContractTests` — builder, cycle rejection and serialization/subscription shape.
   - `RoutingSlipArgumentIntegrationTests` — precedence/fallback matrix, MessageData, object graph,
     nullable default, Uri and custom converter paths.
   - `RoutingSlipLifecycleIntegrationTests` — empty/single/two activity success, exact event data and
     terminal cardinality.
   - `RoutingSlipFaultIntegrationTests` — fault, reverse compensation and compensation-failure paths.
   - `RoutingSlipRetryIntegrationTests` — delayed redelivery and immediate retry with exact attempts,
     variables and compensation recovery.
   - `RoutingSlipRevisionAndSubscriptionTests` — append/discard, activity-added subscriptions,
     content filtering and custom envelope/raw events.
   - `RoutingSlipRequestIntegrationTests` — success, request fault and declared fault response.
   - `RoutingSlipHostConfigurationTests` — complete execute/compensate callback and partition surface.
3. Give every native method one unique `RequirementCoverage` key, bind all 81 obligations to those
   methods, and add the exact method projection to `CoreRequirements.json`.
4. Run the narrow Core project build and only the new Courier carriers while fixing compilation or
   contract errors. Review assertions against the complete event state; never weaken a carrier to
   match a failure.
5. Perform assertion-quality, anti-pattern and pseudo-mutation review. Execute the selected
   single-cause mutations, restore each product file immediately, then run one post-restore control.
6. Delete all 23 fully replaced inherited Courier files in the same Technical commit; verify the
   physical Courier directory is absent and no empty directory remains.
7. Raise the UnitArchitecture floor by the exact 36 materialized cases, from 2,523 to 2,559, in
   workflow, architecture guard, README, `docs/build.md` and this plan.
8. Run once for the complete package: locked Engineering restore, Release build with binlog, focused
   Courier tests, full UnitArchitecture, CI self-tests, identity self-tests, verification model,
   format verification and generated CHANGELIST.
9. Freeze one direct Evidence child containing byte-exact Technical patch, CTRFs, binlogs, raw logs,
   mutation recipes/results and complete SHA-256 inventory; then bind Technical and Evidence hashes
   once in the architecture repository. No remote push without fresh explicit PO authorization.

## Core MessageData native closure — 27-obligation large cohort (2026-08-31)

1. Close exactly `OBL-R0-CORE-C-0103..0129` as one UnitArchitecture package and map all 27
   obligations exactly once to executing native carriers.
2. Replace the repository, threshold, filesystem, encryption, nested-graph, initializer, JSON,
   publish and request/response fixtures with source-mirrored xUnit 4 / MTP v2 tests. Consolidate
   only where one execution observes the complete inherited contract.
3. Bind string, byte array and stream payloads independently; require exact content, address and
   ciphertext-at-rest or path-class oracles as applicable. The large System.Text.Json carrier must
   complete and validate every declared size, including one million values.
4. Use positive receive/request/repository barriers and the configured operation timeout only as a
   failure budget. No sleep, quiet-window or first-message-only success oracle is allowed.
5. Execute a compact independent mutation set over actual MessageData product seams, require the
   intended native carrier to turn red, and restore every product byte exactly after each mutation.
6. Delete the ten fully replaced inherited MessageData files only after focused execution,
   requirement projection and the 27-row terminal map are green. Remove the directory only if it is
   physically empty; never alter the frozen historical expected-core inventory.
7. Raise the UnitArchitecture floor by the exact twenty-two materialized cases, from 2,559 to 2,581;
   the two additional cases bind the public encryption null boundary and owned-stream cleanup.
   Then run one locked Engineering restore/build, one complete UnitArchitecture profile, scoped
   format, CI tools, Identity, Verification Model and CHANGELIST closure for the whole package.
8. Freeze one Technical commit and one direct Evidence child. Independent read-only acceptance,
   architecture binding and remote publication remain separate; no push without fresh explicit PO
   authorization and never force-push.

## Core state-machine exception and observation native closure — 102-obligation cohort (2026-08-31)

1. Close exactly the 102 R0 obligations owned by the declarative and dynamic-builder
   `Exception_Specs.cs` and `Observable_Specs.cs` files. Freeze them in one terminal map before any
   inherited file is removed; every obligation must occur exactly once and target an executing
   xUnit 4 / MTP v2 carrier.
2. Replace the forty state-observation facts with six complete source-mirrored flows: simple
   Initial/Running/Final transitions, substate entry/finalization and substate return with
   BeforeEnter/AfterLeave events, each independently executed through both declarative and dynamic
   machine construction. Assert the complete ordered transition and selected-event sequences, raw
   instance identity and terminal state in one method per flow.
3. Replace the sixty-two exception facts with ten complete flows: typed catch pipelines, base-type
   catch, empty catch continuation and data-event catch for both construction APIs, plus the two
   declarative nested-else/finalize contracts. Bind exact exception type/message, ordered activity
   markers, true/false synchronous and asynchronous branches, exclusivity of the first matching
   catch, skipped post-fault activities and final state.
4. Use direct awaited state-machine execution only. No sleep, timeout-as-success, shared fixture or
   absence-only terminal verdict is permitted. Recorders retain ordered raw callbacks and compare
   complete immutable snapshots after each RaiseEvent returns.
5. Add passive requirement projections for every native method and raise the UnitArchitecture floor
   by the exact sixteen materialized cases, from 2,581 to 2,597. Delete the four inherited fixture
   files only after focused execution, requirement projection and the 102-row map are green. Retain
   shared legacy observer helpers while any other inherited fixture still references them.
6. Bind independent one-cause mutations across typed catch selection, catch continuation,
   data-event catch, state notification, selected-event filtering and substate transition ordering.
   Every mutant must build, turn the intended carrier red and restore the product file byte-exactly.
7. Run focused development loops, then exactly one locked Engineering restore/build, one complete
   UnitArchitecture profile, scoped format and repository gates. Freeze one Technical commit and one
   direct Evidence child. Remote publication remains separately authorized and never uses force-push.

## Core state-machine activities and conditions native closure — 40-obligation cohort (2026-08-31)

1. Close exactly the forty obligations owned by the declarative/dynamic pairs of
   `Activity_Specs.cs`, `AsyncActivity_Specs.cs`, `DataActivity_Specs.cs`, `Condition_Specs.cs` and
   `FilterExpression_Specs.cs`. Freeze one 40-row map; every row must be unique,
   `REPLACED_EXECUTING`, `UnitArchitecture` and point to an existing native method.
2. Add two source-mirrored owners under `tests2/ViciOne.ServiceBus.Tests/SagaStateMachine`: one for
   activities/transitions and one for conditions/filtering. Execute declarative property discovery
   and the dynamic builder as separate Theory rows.
3. Materialize exactly sixteen cases: two each for lifecycle trace, initial-binding equivalence,
   finalization/finally, custom activity, data-event continuation and filter routing, plus four for
   synchronous/asynchronous condition behavior across both construction APIs.
4. Bind exact ordered markers, raw state identities, payload values, branch counters and negative
   branch exclusion after awaited `RaiseEvent` completion. Do not use delays, timeouts as a success
   oracle, manually reset a previously exercised saga or share mutable fixture state.
5. Add passive requirement rows for the seven native methods and raise the UnitArchitecture floor
   by the sixteen materialized cases from 2,597 to 2,613. Delete the ten inherited files atomically
   only after the focused carrier and requirement projection are green; leave the adjacent Group
   package untouched.
6. Run assertion-quality, anti-pattern and pseudo-mutation review, then bind independent one-cause
   product mutations for lifecycle ordering, finalization, activity delegation, data continuation,
   sync/async conditions and filter application. Restore every product byte exactly and rerun the
   focused post-restore control.
7. Pay the broad validation cost once: locked Engineering restore/build, full UnitArchitecture,
   scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical commit and one
   direct Evidence child. No remote push occurs without a new explicit authorization.

## Core state-machine definition/runtime native closure — 120-obligation large package (2026-08-31)

1. Close exactly the 120 R0-CORE-A obligations owned by the 27 frozen definition, state-storage,
   transition, runtime, serialization, observation and visualizer fixtures. Record every obligation
   exactly once in a terminal `REPLACED_EXECUTING` UnitArchitecture map.
2. Add source-mirrored native owners for exact definition/introspection, independent saga state
   properties, raw/string/int state storage and expressions, JSON state round-trip, DuringAny,
   all-state hooks, nested raise, unhandled-event policy and direct transition execution. Exercise
   declarative discovery and the dynamic builder independently.
3. Reuse the accepted complete transition sequence, selected event-observer and canonical visualizer
   owners. Do not create renamed fixtures or new cases for behavior those owners already prove more
   strongly.
4. Materialize exactly 22 cases and raise the independent UnitArchitecture floor from 2,613 to
   2,635. Assertions bind exact state/event sets, runtime types, three reachable events, storage
   values, compiled predicate truth table, serialized state identity, callback order/payload,
   exception type and unchanged negative-path state.
5. After focused build/test and requirement projection are green, delete the 27 fully replaced
   inherited fixtures plus the two now-unreferenced JSON state helpers atomically. Remove only
   directories that become physically empty; never alter the frozen historical expected-core list.
6. Perform assertion-quality, anti-pattern and pseudo-mutation review, then execute independent
   single-cause product mutations across the actual metadata/storage/runtime seams. Restore every
   product byte exactly and rerun one focused post-restore control.
7. Pay broad validation once for the whole package: locked Engineering restore/build, complete
   UnitArchitecture, scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one
   Technical commit and one direct Evidence child. No remote push without new explicit approval.

## Core state-machine composite/recovery native closure — 61-obligation large package (2026-08-31)

1. Close exactly the 61 selected R0-CORE-A obligations owned by the sixteen Combine, CompositeOrder,
   CompositeCondition, Combine_Assigned, CompositeEventMultipleStates, Dependency, Faulted, Retry,
   Telephone and declarative Event fixtures. Keep Group, SubStateOnEnter and dynamic Event questions
   outside this package.
2. Add `StateMachineCompositeEventTests` with five complete source-mirrored flows: constituent/status
   truth table, activity order plus conditional completion, duplicate option behavior, assigned int/
   struct NextEvents surface and cross-state declaration-order equivalence. Execute declarative and
   dynamic construction separately wherever both inherited contracts exist.
3. Add `StateMachineRecoveryTests` for dependency factory/continuation, compensating rollback and the
   four trigger/data x catch/no-catch retry rows. Retry intervals are zero-duration but traverse the
   real interval-policy path; assert four attempts and exact failure/catch identity rather than time.
4. Add `StateMachineTelephoneTests` as six deterministic construction-style x call-path rows. Replace
   Stopwatch and `Task.Delay` with exact Connected enter/leave counts, ordered state markers, number
   propagation and terminal OffHook identity. Reuse the already stronger real-machine visualizer
   owners for the two Draw obligations.
5. Strengthen the existing definition owner with exact `TriggerEvent`/`MessageEvent<T>` runtime-type
   and name assertions. Add nine passive native requirement rows and one exact 61-row terminal map.
   Materialize 26 new cases and raise the UnitArchitecture floor from 2,635 to 2,661.
6. Build/test only the three new carriers and strengthened definition owner while implementing. Run
   assertion-quality and pseudo-mutation review before deletion; never weaken a carrier to match a
   product failure.
7. Bind independent single-cause mutations across composite completion/order/options/metadata,
   dependency continuation, compensation and retry. Restore every product byte immediately and run
   one focused post-restore control.
8. Delete the sixteen fully replaced inherited files atomically only after the 61-row map,
   requirements and focused tests are green. Retain the four question-owned files and observer
   helpers; remove directories only if actually empty and never alter expected/core.txt.
9. Pay broad validation once: locked Engineering restore/build with binlogs, complete UnitArchitecture,
   scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical commit and one
   direct Evidence child; remote publication requires a fresh explicit authorization and never uses
   force-push.

## Core state-machine integration/policy native closure — 74-obligation large package (2026-08-31)

1. Close exactly the remaining 74 R0-CORE-A state-machine obligations owned by the 45 frozen Group,
   request/response, correlation, lifecycle, scheduling, topology, concurrency, fault, missing-instance
   and policy fixtures. Bind all 74 rows exactly once: 69 to executing UnitArchitecture carriers and five
   to explicit invalid-source retirement for two empty RunParallel placeholders, one contradictory Enter
   count and two assertion-free cases. Never invent replacement semantics for invalid legacy tests.
2. Add source-mirrored integration owners for parallel and multi-response requests, correlation/lifecycle,
   configuration, response/fault/outbox, publish/send/dynamic/topology, deterministic scheduling,
   repository concurrency/partitioning and retry/ignore/container policy. Strengthen existing activity
   and runtime owners where the native behavior is already the correct source owner.
3. Materialize exactly 37 cases and raise the independent UnitArchitecture floor from 2,661 to 2,698.
   Tests bind exact state, message, correlation, metadata, exception, retry, response and terminal-event
   identities. Scheduling uses the in-memory delay provider or zero-delay provider path; no carrier uses
   sleep, polling, timeout-as-success or a wall-clock absence oracle.
4. The scheduled-outbox serializer-failure case must prove one exact serializer failure for Count=2,
   committed-state retry, no Count=2 delivery, a real request/response loop, the exact Faulted terminal
   result and retained Failed saga. Deleting the outbox boundary must be killed by the same carrier.
5. Delete all 45 fully disposed inherited files atomically after the 74-row map, requirement projection,
   Release build and 63-case focused carrier set are green. Remove the physically empty Dynamic Modify
   directory; preserve nonempty inherited directories and never alter expected/core.txt.
6. Perform assertion-quality, anti-pattern and pseudo-mutation review across every new owner. Execute
   independent, buildable one-cause mutations for each high-value behavior axis, retain passing controls
   where axes share a Theory, restore every product/test byte exactly and rerun the focused control.
7. Pay broad validation once for the completed package: locked Engineering restore/build, complete
   UnitArchitecture, scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical
   commit and one direct Evidence child. Remote publication requires a fresh explicit authorization and
   never uses force-push.

## Core container/runtime native closure — 56-obligation large package (2026-08-31)

1. Close exactly the 56 selected R0-CORE-B obligations owned by AccessScope, Dispatcher, EmptyBody,
   EndpointConfiguration, endpoint/type exclusion, Handler, HealthCheck, MediatorFilter, MultiBus scope,
   endpoint dependency, redelivery/request headers, Scheduler, Stop and TenantScope. Keep Future and the
   unrelated saga-state-machine request fixture outside this package.
2. Bind every obligation exactly once in `core-container-runtime-native-obligation-map.tsv`. Materialize
   54 cases across source-mirrored dependency-injection, transport, mediator, scheduling, middleware and
   Courier owners; reuse the stronger existing direct transport-stop carrier and allow one MultiBus owner
   to carry the two independently asserted provider/setter obligations.
3. Raise the independent UnitArchitecture floor from 2,698 to 2,752. Exact test results must remain above
   the floor; the first post-deletion full run is 2,757/2,757 across 21 CTRFs with no failure or skip.
4. Tests must bind actual product boundaries: exact AddHandler overload/dependency selection, endpoint
   probe precedence, positive-control endpoint exclusion, owning DI scope, bound-bus isolation, dependency
   readiness followed by terminal drain, exact raw/empty dispatch, health transitions, scoped scheduling,
   tenant resolution order, retry cancellation and Courier header isolation. No sleep, polling or
   timeout-as-success oracle is permitted.
5. Delete exactly the sixteen fully replaced inherited ContainerTests files only after all carriers,
   requirement rows, Release build and focused runs are green. Preserve `Future_Specs.cs`, every other
   open legacy fixture and the frozen expected-core list; remove a directory only if physically empty.
6. Perform assertion-quality, anti-pattern and source-derived pseudo-mutation review across the complete
   carrier set. Execute independent buildable one-cause mutations for the high-value DI, configuration,
   dispatch, scheduling, cancellation and Courier seams; bind targeted red cases and passing controls,
   restore every byte exactly and rerun the focused post-restore cohort.
7. Pay the broad validation once after mutations: locked Engineering restore/build with binlogs, complete
   UnitArchitecture, scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical
   commit and one direct Evidence child. Do not push remotely without a new explicit authorization and
   never force-push.

## Core container common/scenarios native closure — 102-obligation large package (2026-09-01)

1. Close exactly the 102 R0-CORE-B obligations `0040..0133` and `0204..0211` owned by the inherited
   ContainerTests Common_Tests and Scenarios directories. Bind all rows exactly once: 97 to executing
   UnitArchitecture carriers and five abstract, nonexecuting source identities to explicit invalid-source
   retirement. Never invent execution semantics for abstract fixtures with no concrete cohort owner.
2. Materialize 50 new deterministic cases across consume/outbox context, consumer lifetime and filters,
   endpoint precedence, Courier, discovery, JobService, Mediator, saga/state-machine scope, scoped
   endpoints, request clients, MultiBus and batch futures. Reuse stronger existing native carriers only
   where they assert the complete inherited contract.
3. Raise the independent UnitArchitecture floor from 2,752 to 2,802. The focused package consists of
   the 50 new cases plus existing shared carriers; every async absence claim must be bounded by a positive
   product-owned barrier, never sleep, polling or timeout-as-success.
4. Preserve exact ownership boundaries: one consume/outbox object identity, exact scope disposal and
   filter layers, exact endpoint/source addresses, request causation, three distinct MultiBus owners and
   clients, terminal saga/future events, and no side-effect flush after fault.
5. Delete exactly the forty fully disposed inherited files only after the 102-row map, requirement
   projection, Release build and complete focused package are green. Remove both physically empty legacy
   directories and never edit `build/verification/expected/core.txt`.
6. Perform assertion-quality, anti-pattern and source-derived pseudo-mutation review across every new
   owner. Execute independent buildable one-cause mutations for the high-value DI, outbox, routing,
   MultiBus, state-machine, Courier and future seams; restore every byte exactly and rerun the focused
   post-restore cohort.
7. Pay broad validation once after mutations: locked Engineering restore/build with binlogs, complete
   UnitArchitecture, scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical
   commit and one direct Evidence child. Remote publication requires a fresh explicit authorization and
   never uses force-push.

## Core final inherited-project native closure — 65-obligation terminal package (2026-09-01)

1. Close the final 65 R0 rows owned by `tests/ViciOne.ServiceBus.Tests`: 55 executable Saga,
   state-machine request, convention, Future, reliable-messaging and removal/network obligations plus
   ten R0 support rows that possess no executable anchor. Bind the executable rows as
   `REPLACED_EXECUTING`; retire the support rows explicitly without inventing test behavior.
2. Materialize 34 deterministic cases in source-mirrored native owners. Reuse the stronger accepted
   partitioned saga and container-saga carriers. Consolidate the inherited Future scenarios only at
   their shared observable fork/join, success/fault, durable replay and registration-helper boundaries.
3. Require positive transport or consumer completion before every terminal count. Bind exact saga
   instance, response/fault, routing-key, retry-attempt and durable-replay identities. No sleep,
   polling, quiet-window or timeout-as-success assertion is permitted.
4. Correct the in-memory outbox attempt boundary so a failed consumer attempt discards its pending
   messages before retry. Prove exactly-once committed output across the retry and kill a one-cause
   mutation that removes only this discard operation.
5. Raise the independent UnitArchitecture floor from 2,802 to 2,836. Delete all 65 tracked inherited
   project paths only after the 65-row map, passive requirement projection, Release build and focused
   native carrier set are green. Verify that `tests/ViciOne.ServiceBus.Tests` is physically absent and
   leave `build/verification/expected/core.txt` unchanged.
6. Perform assertion-quality, anti-pattern and source-derived mutation review over the complete new
   owner set. Bind independent buildable one-cause mutations for the high-value saga, convention,
   Future, reliable/outbox and architecture boundaries; restore every byte exactly and rerun the
   focused post-restore control.
7. Pay broad validation once after mutations: locked Engineering restore/build with binlogs, complete
   UnitArchitecture, scoped format, CI/Identity/Verification Model and CHANGELIST. Freeze one Technical
   commit and one direct Evidence child. Remote publication remains separately authorized and never
   uses force-push.

## Event Hubs native closure — 25-obligation provider package (2026-09-01)

1. Close the 25 Event Hubs R0 obligations: 22 executable LocalIntegration contracts and exactly three
   real-Azure-only contracts (`0258`, `0261`, `0262`) as visible `EXTERNAL_PENDING` work. Never promote
   emulator authentication, rebalance or retention behavior to a real-service verdict.
2. Use the pinned Event Hubs emulator together with run-scoped Azurite checkpoint storage. Prove exact
   batch cardinality/order, raw-SDK interoperability, envelope and provider metadata, retry/fault paths,
   checkpoint confirmation, multi-rider isolation, saga activities and partition lifecycle without sleep,
   polling, quiet-window or timeout-as-success oracles.
3. Correct the shared rider lifecycle so every rider generation is stopped exactly once after repeated
   bus start/stop. Carry both a hermetic two-generation regression owner and the real provider callback
   proof; bind a one-cause mutation that restores the one-shot collection stop and makes both red.
4. Raise UnitArchitecture from 2,836 to 2,838 and LocalIntegration from 375 to 398. The Event Hubs project
   contributes 23 behavior cases plus one passive requirement-projection case. The additional Unit owner
   proves that a convention enumeration acquired before registration remains an immutable snapshot.
5. Delete the complete inherited Event Hubs test project only after projection, Release builds, all 23
   native cases and targeted mutations are green. Preserve the frozen expected-core identity list and
   keep the three External obligations nonexecuting and visible.
6. Pay the broad Engineering, UnitArchitecture, LocalIntegration, CI-tool, identity and change-list cost
   once after mutation restoration. Freeze a Technical commit and direct Evidence child; do not push
   without a fresh explicit authorization and never force-push.

## Azure Service Bus native closure — 153-obligation capability-first provider package (2026-09-01)

1. Re-derive all 153 frozen Azure Service Bus obligations from the current provider boundary. The old
   31 UnitArchitecture / 15 LocalIntegration / 107 External split predates emulator 2.0.0 and is not an
   execution verdict. Pin Microsoft's official multi-architecture 2.0.0 image by manifest digest, run it
   with the existing supported SQL Server fixture and measure the administration and AMQP capabilities
   before assigning any final disposition.
2. Add one run-scoped, loopback-only `servicebus` fixture with ephemeral AMQP and HTTP management ports,
   EULA acceptance, bounded health, exact image/config binding, canonical runner projection and complete
   teardown/log capture. No Azure credential fallback, shared namespace or persisted fixture state is
   allowed. The emulator and other heavyweight providers remain sequential categories.
3. Probe queue/topic/subscription/rule create-update-delete, forwarding, sessions/session-state,
   scheduling/cancellation, duplicate detection, dead-lettering and quota boundaries through the real
   `Azure.Messaging.ServiceBus` 7.20.1 data and administration clients. Classify an obligation as local
   only when the emulator executes the same observable provider contract. Keep Entra/RBAC, partitioning,
   AMQP WebSockets, tier/availability, real-service retention and other unsupported semantics visibly
   `EXTERNAL_PENDING`; never count them green.
4. Create source-mirrored native UnitArchitecture, LocalIntegration and External owners from the measured
   capability matrix. Tests bind exact entity/message/session/sequence/dead-letter/forwarding identities
   and provider state at positive completion barriers. No sleep, polling, quiet-window, timeout-as-success,
   reflection-only product oracle or self-derived expected value is permitted.
5. Preserve all inherited Azure Service Bus files that still own an unimplemented or External obligation.
   Delete a file only when every one of its frozen obligations has a stronger executing or explicitly
   External native owner; delete the complete old project only after all 153 rows are terminal. Never edit
   the frozen expected-core identity list and remove only directories that are physically empty.
6. Correct product defects exposed by the carriers instead of weakening assertions. Bind independent,
   buildable one-cause mutations for each high-value address, topology, configuration, send-property,
   settlement, session, scheduling, forwarding, retry and validation mechanism; restore every byte exactly
   and rerun the focused positive control after mutation work.
7. Pay locked restore/build, complete UnitArchitecture and LocalIntegration, scoped format, Engineering,
   CI-tool, identity, verification-model and CHANGELIST costs once for the finished package. Freeze one
   Technical commit and one direct Evidence child with raw CTRF/log/binlog/hash evidence, perform the
   independent static counterexample review, then publish without force-push under the current explicit
   remote-backup authorization.

## Final A+ native-test promotion — two large completion packages (2026-09-02)

### Package A — benchmark and legacy capability closure

1. Port the exact 75 frozen NUnit benchmark cases into the existing benchmark xUnit-v4/MTP project.
   Preserve data-row cardinality and strengthen the three wall-clock carriers with a deterministic
   metric-clock seam that leaves the production Stopwatch clock unchanged.
2. Add method-level requirement projection and an exact inherited-identity disposition. Serialize
   process-global environment/Console tests and require exact exceptions, state, effective provider
   settings, metric cardinality and completion identities.
3. Run the narrow Release build/test and targeted one-cause mutations, then delete the old benchmark
   test project and remove it from Engineering, package management and the verification model.
4. Create a path-complete TestFramework capability disposition, map reusable infrastructure and domain
   behaviors to their accepted native owners, verify no consumer remains, then remove the project and
   its root-solution membership atomically.

### Package B — one test tree and one CI truth

5. Retire the legacy verification model, VSTest/NUnit/Python-unittest verdict stack and obsolete workflow
   jobs. Preserve provider runner utilities only where the native workflow consumes them, and protect
   their contracts from the native Architecture project.
6. Fold restore/build/test/provider/pack/identity gates into one native workflow. Keep real-cloud-only
   Azure/AWS obligations explicitly External Pending; they are visible follow-up work, never counted as
   local green or as a blocker for the structural promotion.
7. Atomically replace the placeholder `tests/` tree with the complete `tests2/` native tree. Mechanically
   update active source, MSBuild, solution, workflow, tool and documentation references; keep frozen
   historical evidence immutable and append explicit supersession where needed.
8. Run locked restore, non-incremental Release Engineering build, complete UnitArchitecture and available
   LocalIntegration/provider categories, format/static analysis, identity/change-list and mutation
   controls. Freeze technical and evidence commits, verify a clean worktree except the user-owned
   untracked `review/` directory, and publish without force-push under the standing explicit permission.

### Completion state — 2026-09-02

Packages A and B are implemented and locally accepted. All technical, test, provider, quality and
mutation gates are complete. The remaining mechanical closeout is the package/identity evidence
freeze, two commits and the already authorized non-force remote publication.

## Reviewer integration — V4 telemetry and fault envelopes (2026-09-02)

1. Freeze the current product commit/tree and reviewer hashes; preserve `review/**` byte-for-byte and
   use the donor only as semantic evidence.
2. Add one internal no-throw Activity observation boundary and route activity creation, start, tag,
   event, status and stop through it. Preserve ambient Activity ID, trace state and nonreserved baggage
   when no child Activity can be created or started.
3. Make fault projection finite and serialization-safe: at most 16 aggregate exceptions, 16 inner
   levels, 32 distinct case-insensitive data keys, 256 characters per key and 2,048 characters per
   diagnostic text. Preserve wrapper precedence and remote exception identity.
4. Extend the two existing native owners with exact positive, over-limit, malformed/custom-value and
   listener-failure contracts. Update the passive requirement projection; do not create a second test
   project or resurrect an inherited test.
5. Run the focused Release build/tests first, then one-cause mutants for both behavior families. After
   byte-exact restoration, run the complete UnitArchitecture profile, scoped format and applicable
   repository static gates once. Record raw evidence under
   `evidence/WP-F2-SERVICEBUS-REVIEW-INTEGRATION-01/V4-TELEMETRY-FAULTS/`.

## Reviewer integration — V4 bounded execution and task primitives (2026-09-02)

1. Replace the overlapping channel executors with a single hard-bounded `TaskExecutor` and a
   lazily materialized, per-partition bounded `PartitionedTaskExecutor`.
2. Split the former catch-all task utility by responsibility and migrate every production and
   benchmark consumer atomically. Keep Task and ValueTask delegate APIs unambiguous.
3. Bind exact concurrency, FIFO, admission, cancellation, result, fault ownership, logger isolation,
   synchronous-wait, partitioning and concurrent-disposal behavior with ordinary native xUnit tests.
4. Reject one independent cause for every critical mechanism, restore each target exactly, then run
   the zero-warning Release build, complete UnitArchitecture profile and scoped format gate.
5. Preserve this as a local checkpoint while the same authorized assignment continues with explicit
   receive startup, retry, terminal-fault, cancellation and stop semantics.

## Reviewer integration — V4 explicit receive terminality (2026-09-02)

1. Carry a non-null exact exception and explicit terminality through receive transport and endpoint
   fault contracts; make the retry owner the only authority for terminal exhaustion.
2. Make endpoint start, readiness cancellation and synchronous rollback deterministic, preserving the
   exact initiating cancellation token and exact terminal cause.
3. Own in-memory startup as a task, publish dependency failures as terminal, and make stop await the
   startup-observation path without allowing another active agent to mask the test oracle.
4. Bind attempt, exhaustion, stop, bus-waiter and in-memory lifecycle behavior with ordinary native
   tests and independent one-cause product mutations.
5. After restoration, require focused owners, a zero-warning Release build, the complete
   Unit/Architecture profile, passive requirement projection and scoped static gates to pass before the
   local checkpoint is committed.

## Reviewer integration — V4 typed EF bus-outbox reliability (2026-09-02)

1. Type notification, session, scoped context and operational ownership by bus plus `DbContext`; make
   multiple registrations select exactly one explicit default or fail before a session is created.
2. Freeze validated delivery settings at registration completion and make commit/abort behavior explicit,
   including ordinary business-only commits and durable-before-notification ordering.
3. Persist bounded retry and quarantine state with isolated transport classification, corrupt-metadata and
   missing-destination terminality, deterministic bus-scoped administration and provider-exact SQL.
4. Exercise retry due time and concurrent PostgreSQL delivery against the real local provider. Use
   `ReadCommitted` with skip-locked workers, preserve explicit later overrides and require all 144 messages.
5. Bind exact hermetic state tests, real provider runs and at least sixteen independent one-cause mutations;
   strengthen any surviving oracle before accepting the package.
6. Freeze a local product commit and architecture binding as V4 package 5/12. Keep `review/**` untouched and
   do not publish remotely without fresh explicit permission.
