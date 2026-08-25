# Research — native source-owner reconstruction

## Sources read

- the relevant implementation under `src/ViciOne.ServiceBus.Abstractions`;
- all 19 C# files and the 549-value formatter corpus under the inherited
  `tests/ViciOne.ServiceBus.Abstractions.Tests` project;
- all 78 final semantic-ledger entries owned by that inherited project;
- the complete existing native test tree, its package graph, build rules, and requirement verifier;
- the mandatory Microsoft .NET test and MSBuild work rules.

## Disposition

The product behavior is split by resource boundary:

- deterministic exception-filter, type-extension, NewId value, formatting, parsing, generation,
  ordering, interoperability, and concurrency behavior belongs to `UnitArchitecture`;
- the two providers that read the actual host name or physical network interfaces belong to
  `LocalIntegration`;
- adapter enumeration/console-output cases, a BCL `ArgumentNullException.Message` observation, and
  compile-disabled ordering experiments are not product verdicts and are not recreated as tests.

The 549-value formatter corpus is embedded in the new assembly byte-for-byte. Random Guid and clock
inputs were replaced with fixed external values. Generation tests use injected tick, worker, and
process providers; the concurrency contract uses a private deterministic generator instead of
mutating the process-wide static generator. All executable methods carry passive requirement
metadata and are checked against one projection per assembly.

The original behavior cohort changed no production source or inherited test. After its complete
78/78 disposition was accepted, a separate cleanup removed the fully replaced NUnit project. Its
compile-only usage types moved into the source-structured, non-packable OrderWorkflow sample.

## MessagePack owner

The complete MessagePack product source, all inherited serialization fixtures assigned to that
owner, and all 67 frozen semantic-ledger obligations were read before implementation. Six additional
MessagePack body variants were found under the inherited mixed core fixture and reassigned to their
actual source owner. The old parameterized fixtures mixed JSON, core pipeline behavior, and
MessagePack behavior; the native
replacement assigns each assertion to the MessagePack source owner and tests the two genuine
cross-component boundaries through the real in-memory bus.

The replacement covers envelope metadata, scalar and contractless shapes, collections, interfaces,
formatter-cache concurrency and weak-key behavior, jobs, faults, MessageData, opaque XML payloads,
clone/redelivery behavior, configuration, and the hardened single-options-owner rule. The 67 old
obligations are mapped individually in
`evidence/native-tests/messagepack/INHERITED_BEHAVIOR_DISPOSITION.json` and
`evidence/native-tests/messagepack/INHERITED_MESSAGE_BODY_DISPOSITION.json`. No inherited fixture,
NUnit base class, or TestFramework helper is reused.

## State-machine visualizer owner

The entire three-file visualizer product source and all nine inherited obligations assigned to that
owner were read before implementation. The replacement retains byte-exact Graphviz and Mermaid
contracts with expected documents independent of the generator, exercises declarative and dynamic
state-machine inputs, verifies all request-derived outcomes, proves composite-event edge filtering,
and defines the empty-graph boundary. The inherited no-assertion console-output cases are replaced
by behavioral assertions. Each obligation is mapped in
`evidence/native-tests/state-machine-visualizer/INHERITED_BEHAVIOR_DISPOSITION.json`; no inherited
fixture or TestFramework helper is reused.

## Endpoint-name formatter cohort

Baseline `d06874d7` was clean before cohort research. The complete inherited
`tests/ViciOne.ServiceBus.Tests/EndpointName_Specs.cs`, all 17 final R0 ledger obligations owned by
that file, and the complete current implementation boundary were read: the three endpoint-name
formatters, `IEndpointNameFormatter`, type-name formatting, endpoint settings, the consumer endpoint
definition, and `ConfigurationException`. The complete tracked native test estate and its effective
MSBuild, package, MTP, configuration, solution, and CI files were already read and hash-checked by
the immediately preceding retrospective audit; no test or build file changed after that audit.

The source owner is `src/ViciOne.ServiceBus/Configuration`. Its native tests therefore belong under
`tests2/ViciOne.ServiceBus.Tests/Configuration/EndpointNaming`. The inherited instance-id case is a
deliberate cross-assembly contract: `ConsumerEndpointDefinition` composes the concrete kebab
formatter's separator and sanitizer. It remains in this cohort, while later standalone behavior of
the abstract endpoint-definition hierarchy belongs to the Abstractions owner.

Acceptance checklist:

- preserve all 17 inherited naming behaviors with literal, implementation-independent string or
  exact-exception oracles;
- retain word, digit, acronym, nested-namespace, prefix, generic-consumer, message, instance-id, and
  reserved-suffix boundaries;
- use local deterministic contracts instead of the inherited TestFramework message types;
- map every inherited obligation to a concrete native xUnit/MTP replacement;
- never weaken an expected value to fit product behavior; a reproduced product defect is corrected
  minimally in product code and protected by a positive regression plus a targeted mutation;
- remove the inherited file only after all 17 obligations have terminal replacements;
- raise the predeclared UnitArchitecture floor by exactly the 17 newly materialized cases.

## Message URN cohort

Baseline `4a51998f` is clean and byte-identical to its private remote branch. The complete product
sources `MessageUrn.cs` and `Attributes/MessageUrnAttribute.cs`, the complete inherited
`MessageUrnSpecs.cs` and `MessageType_Specs.cs`, and all 15 relevant final R0 ledger obligations were
read before implementation. The set consists of 12 executing MessageUrn fixture behaviors, two
attribute behaviors scattered into MessageType specs, and the explicit current-product gap for
`MessageUrn.Deconstruct`. The array publish/consume behavior in `MessageType_Specs.cs` is not a URN
derivation contract and remains outside this cohort.

The source owner is `src/ViciOne.ServiceBus.Abstractions`. The native replacement is therefore split
between project-root `MessageUrnTests.cs` and `Attributes/MessageUrnAttributeTests.cs` in
`tests2/ViciOne.ServiceBus.Abstractions.Tests`, mirroring the two product files instead of copying the
old core-test placement. Invalid attribute values are asserted directly against the public
attribute constructor. The inherited `TypeInitializationException` wrapper came only from the
generic static cache and is not a product validation contract.

Acceptance checklist:

- retain exact plain, nested, closed-generic, attributed, attributed-array, custom-scheme, Unicode,
  punctuation, and open-generic behavior, with consistent null rejection on both runtime overloads;
- retain null, empty, whitespace, duplicate-default-prefix, and invalid-custom-URI rejection with
  exact exception types and stable product-owned details;
- cover all four `Deconstruct` shapes: name only, namespace plus name, assembly-bearing, and a
  non-message scheme;
- require both runtime-type overloads to reject an open generic consistently;
- map all 15 inherited/current-product obligations to concrete native methods;
- delete only `MessageUrnSpecs.cs` in this cohort; retain `MessageType_Specs.cs` until its independent
  array publish/consume obligation is replaced, which C9 subsequently completed;
- change product code only if a predeclared native assertion first reproduces a real defect;
- add exactly 17 materialized UnitArchitecture cases and raise its floor from 635 to 652 before the
  first cohort result exists.

## Request-rate algorithm cohort

Baseline `5d3e9602` is clean and byte-identical to its private remote branch. The complete product
sources `Util/RequestRateAlgorithm.cs`, `Util/RequestRateAlgorithmOptions.cs`, and
`Util/ActiveRequest.cs`, the complete inherited `PollingAlgorithm_Specs.cs`, and all seven relevant
R0 obligations were read before implementation. Six obligations come from executing inherited
tests; the seventh is the already recorded zero-value constructor gap.

The inherited flow-control test contains no assertion. The grouped test uses unseeded randomness and
wall-clock delays to make requests overlap. The native replacement must preserve their meaningful
behavior without preserving either defect: explicit result sets and callback counts replace the
assertion-free case, while task-completion barriers create deterministic request overlap without
sleeping. Its result-capacity limit is deliberately higher than the request window, so asynchronous
result-task teardown cannot contaminate the request-concurrency measurement. The source owner is
`src/ViciOne.ServiceBus.Abstractions/Util`, so the replacement belongs under
`tests2/ViciOne.ServiceBus.Abstractions.Tests/Util`.

Acceptance checklist:

- prove the generic `Run` overload requests the configured limit, returns the exact result count,
  and processes every returned value;
- execute the grouped/ordered overload five times with deterministic groups, prove every pass,
  leave no active request, and reach the configured maximum concurrency of ten;
- retain the exact full-batch scaling curve `1, 6, 8, 9, 10, 10`;
- retain prefetch clamping and both full/empty single-request boundaries;
- reject zero `PrefetchCount` and zero `RequestResultLimit` with the stable product-owned reason;
- map all seven obligations to six native methods materializing eight cases;
- remove only `PollingAlgorithm_Specs.cs` after all seven rows close;
- add exactly eight UnitArchitecture cases and raise its floor from 652 to 660 before the first
  cohort result exists.

## Reflection accessor and static-property cohort

The complete product sources `ReadWriteProperty.cs`, `ReadWritePropertyCache.cs`, and the static-
property portion of `TypeExtensions.cs`, both duplicated inherited `FastProperty_Specs.cs` files,
the complete inherited `StaticProperty_Specs.cs`, and their ten R0 rows were read before editing.
The duplicate FastProperty fixture does not justify duplicate tests: both source pairs map to the
same two behavior carriers. The five static-property cases stay separate because each fixes a
different accessibility or inheritance boundary.

R0 gap `OBL-R0-CORE-D-0477` describes a `KeyNotFoundException` from `TryGetValue`, but that contradicts
the established .NET Try pattern and the method's actual public behavior. `Dictionary.TryGetValue`
does not throw for a missing key, so the catch in the product method is unreachable. The A+ contract
is `false` plus a null output; the dead catch is removed and that contract receives an executable
negative test. No existing behavior changes.

`ImplementedTypeCache_Specs.cs` was deliberately excluded. Its lone count assertion does not prove
which implemented interface or `direct` value is semantically correct, and the current algorithm is
sensitive to interface-enumeration order. Freezing a guessed topology contract would be worse than
leaving the obligation visibly open for its own topology cohort.

## Implemented-message topology cohort

The topology contract is no longer guessed. The inherited regression originates in the MassTransit
fix for interface faults hidden behind a base class: the base class and its independently valid,
most-specific interfaces must remain direct topology edges. For an interface hierarchy, only the
immediate most-specific parents are edges; for a diamond, their shared ancestor is reached through
those parents and must not be duplicated. `Fault<T>` projects the same direct edges to `Fault<P>` and
also retains the non-generic `Fault` contract.

The inherited implementation achieved this through mutation of one shared set while iterating the
unspecified order returned by reflection. The replacement computes the maximal valid interface set
explicitly and orders it by stable type identity. Six source-owner cases bind exact types, uniqueness,
and the `direct` bit; the former count-only obligation maps to the interface-chain case.

## Message initializer object-graph cohort

Baseline `0e993020` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Initializers/Class_Specs.cs`, its six final R0 ledger obligations,
and the complete initializer path that selects property providers, creates dynamic interface
implementations, recursively initializes nested values, and copies exact values were read before
the first test edit. The fault projection additionally crosses the current `FaultEvent<T>` and
`Fault<T>` contracts and therefore remains an end-to-end initializer test rather than a converter
unit test.

The inherited nested-object and fault cases contain useful assertions. Its four read-only-property
cases merely await initialization and would remain green for wrong output. Their replacements use
the same four source/target shapes but assert the actual writable and computed values. Anonymous
input into an interface deliberately does not invent a value for an absent read-only source
property; the test proves successful initialization and the supplied writable value without
freezing the generated implementation's default backing value. Concrete input into an interface
does expose the computed read-only property and must preserve it. Concrete targets preserve the
computed value through their writable property. No reference-identity promise is inferred for
recursively projected faults or messages.

Acceptance checklist:

- map all six inherited obligations one-to-one to six ordinary xUnit methods;
- prove covariant fault projection from `Fault<Top>` into `Fault<Bottom>`, including deterministic
  host values and nested message content;
- prove recursive anonymous-object initialization into private setters;
- prove observable interface and concrete-class results for anonymous and concrete inputs with a
  read-only computed property;
- use fixed external values and the central xUnit cancellation token; use no inherited fixture,
  TestFramework helper, clock assertion, random value, sleep, or environment metadata;
- reject independent nested-conversion, fault-projection, read-only-copy, and requirement-metadata
  mutations before closing the cohort;
- raise the predeclared UnitArchitecture floor from 747 to 753 and delete `Class_Specs.cs` only
  after all six ledger obligations have terminal dispositions.

## Dictionary and ExpandoObject initializer cohort

Baseline `c9409009` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Initializers/Expando_Specs.cs`, all six final R0 ledger obligations,
and the owning initializer implementation were read before editing. The file contains three
different source owners that must not be copied into one generic test class:

- dictionary and `ExpandoObject` message initialization belongs to
  `Initializers/Conventions/DictionaryInitializerConventionTests`;
- explicit dictionary-to-contract converter availability belongs to
  `Initializers/PropertyProviders/PropertyProviderFactoryTests`;
- the concrete nested DTO case belongs to the existing root
  `Initializers/MessageInitializerObjectGraphTests`.

The inherited ExpandoObject scalar case covers long-to-enum and name-to-enum conversion as well as
fixed scalar and Guid copying. The nested list case deliberately fixes the runtime input shape as
`ExpandoObject` plus `List<object>` plus long scalar values; it must assert the list count and
all exposed nested values, not merely presence. The large DTO case uses an exact concrete property
type, so its observable A+ contract is exact object preservation across dynamic interface creation,
not an invented deep-clone promise. All inherited random Guids and current-clock input are replaced
by fixed external values.

Acceptance checklist:

- map all six inherited obligations one-to-one to six ordinary xUnit methods under their real
  source owners;
- preserve ExpandoObject scalar, enum-by-value, enum-by-name, and Guid initialization;
- prove the public provider factory returns a usable object-to-contract converter for a dictionary
  input type;
- preserve the complete concrete nested DTO through a dynamic message interface with fixed
  correlation and timestamp input;
- preserve plain dictionary input and a dictionary-valued nested message property;
- preserve the nested ExpandoObject/list runtime shape with exact list count, identifiers, scalar
  values, and nested product values;
- reject independent enum, dictionary lookup, nested-object, nested-list, and requirement-metadata
  mutations before closure;
- raise the predeclared UnitArchitecture floor from 753 to 759 and remove `Expando_Specs.cs` only
  after all six rows have terminal dispositions.

## Message-initializer capability cohort

Baseline `0461535b` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Initializers/MessageInitializer_Specs.cs`, its 27 final R0 ledger
obligations, the public initializer/cache/factory/builder path, and the collection, task, variable,
exception, scalar, and nested-object conversion owners were read before editing.

The inherited file combines three genuinely end-to-end request/response cases with a large shared
one-time fixture whose remaining 23 tests each assert one facet of the same initialized object. A
greenfield replacement must retain the request/response boundary where it is the behavior, but must
not retain shared mutable fixture state, console output, random/current-time assertions, or one old
test per assertion. Pure conversion behavior is exercised directly through `MessageInitializerCache`
under the actual converter or initializer source owner.

Thirteen independent facts cover the 27 obligations without semantic loss:

- three real in-memory request/response facts own value merging, missing response properties, and
  nullable numeric round trips;
- one object-graph fact owns partial `ExceptionInfo` initialization;
- one dictionary-converter fact owns exact dictionaries, key/value conversion, and nested values;
- one scalar matrix owns fixed DateTime, decimal/string round trips, enums, nullable numeric
  boundaries, exact strings, and Uri/string conversion;
- one array-converter fact owns scalar conversion, exact arrays, and nested interface elements;
- one list-converter fact owns enumerable and array values exposed through list contracts;
- separate object-graph facts own most-derived duplicate-property selection and nested interface
  initialization;
- exception, initializer-variable, and nested-task facts each live with their corresponding
  converter owner.

`InVar.Timestamp` is observed through the exact value captured by its returned variable; no
before/after clock comparison is permitted. `InVar.Id` is asserted by non-default and equality
relationships within one initialize context, which is the product behavior. All transport waits use
the central operation timeout as a safety boundary only.

Acceptance checklist:

- map all 27 inherited obligations to the 13 executing source-owner facts;
- use the real request client and response initializer for the three transport-bound behaviors;
- use fixed external values for all ordinary inputs and never use sleep, polling, random test input,
  or elapsed time as an oracle;
- prove exact collection count, order, keys, values, and nested members;
- prove exception information, duplicate-property selection, variable consistency, and nested-task
  completion through observable output;
- reject one-cause mutations across the pipeline, collection, scalar, object, variable, task, and
  requirement-projection paths;
- raise the UnitArchitecture floor from 759 to 772 and remove only
  `MessageInitializer_Specs.cs` after all 27 ledger rows are terminally mapped.

## Property-provider factory cohort

Baseline `72dfdcb5` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Initializers/PropertyProvider_Specs.cs`, its 42 final R0 ledger
obligations, all 836 lines of `PropertyProviderFactory<TInput>`, every property-provider class, and
the collection, nullable, type, object-initializer, task, and variable converters selected by the
factory were read before the first test edit.

The inherited file is a flat list of one test per source/target pairing. Its value is the complete
pairing matrix, not that structure. A greenfield replacement groups the same observable cases by
the factory branch and concrete provider that owns them:

- four facts cover task-valued properties, arrays containing tasks, task-valued array/list shapes,
  and task-valued result types;
- two facts cover exact and converted arrays plus enumerable sources, including null/empty element
  boundaries;
- two facts cover dictionaries, independent key/value conversion, key/value-pair enumerables, and
  dictionary-held nested contracts;
- three facts cover exact/nullable/object/string scalar paths, all enum representations, and the
  exact Uri/string round trip;
- three facts cover direct and nested interface initialization, exception information, and
  initializer-variable result conversion;
- one additional hardening fact proves that an unsupported source/target pair returns `false` and
  no provider instead of creating a false-positive provider.

The replacement uses a single small test-only context reader that performs the public
`TryGetPropertyProvider` call and executes the returned provider in a real `InitializeContext`. It
is fixture plumbing only: no receipt, interceptor, test verdict, discovery, retry, clock, or hidden
assertion mechanism. Each behavior fact keeps ordinary xUnit assertions and a passive requirement
identity.

Acceptance checklist:

- map all 42 inherited obligations to 14 executing behavior facts and classify the unsupported-pair
  fact independently as new hardening;
- preserve every source/target shape, including array-of-task null defaulting, empty-string to
  nullable element, all task result surfaces, multiple result types for one property, dictionary
  key/value conversion, and nested interface collections;
- keep exact values, cardinality, order, keys, nested members, and nullable boundaries observable;
- place tests under `Initializers/PropertyProviders`, mirroring the owning product subtree;
- kill independent one-cause mutations in the task, array, dictionary, scalar, enum, Uri, object,
  exception, variable, unsupported-pair, and passive-requirement paths;
- raise the UnitArchitecture floor from 772 to 787 and remove only
  `PropertyProvider_Specs.cs` after all 42 ledger rows are terminally mapped.

## Agent lifecycle and pipe-context cache cohort

Baseline `27f782b2` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Middleware/Agents/Agent_Specs.cs`, its six final R0 ledger
obligations, `Agent`, `Supervisor`, every `Agents/*PipeContext*` implementation, both supervisor
extension surfaces, and the corresponding interfaces were read before the first test edit.

The inherited file mixes two different source owners. A greenfield replacement therefore splits
it without changing the six observable contracts:

- four lifecycle facts belong to the Abstractions owner `Middleware`: ready failure propagation,
  an empty supervisor stop, recursive supervisor/agent shutdown, and stopping an agent added after
  readiness;
- two cache facts belong to the Core owner `Agents/PipeContextSupervisor`: a pipeline exception and
  an explicit context invalidation both dispose the cached context so the following send obtains a
  new context.

The frozen ledger prose says that a pipe fault does not invalidate the cache, but its own expected
third-send value is `2`. The inherited code and product implementation prove the opposite: the
fault calls `ActivePipeContextHandle.Faulted`, which disposes the underlying handle, and
`GetContext` then creates context `2`. The replacement preserves the executable behavior and
corrects only that contradictory sentence; no behavior is dropped.

Acceptance checklist:

- map all six inherited obligations one-to-one to six ordinary xUnit/MTP facts;
- preserve the 50-iteration ready-fault race exposure without sleeps, random input, or wall-clock
  assertions;
- replace the three assertion-free lifecycle cases with explicit Ready, Completed, Stopping,
  Stopped, count, and child-state assertions;
- use deterministic context IDs and synchronous test-owned disposal accounting for both cache
  invalidation paths;
- place lifecycle tests under the Abstractions `Middleware` owner and cache tests under the Core
  `Agents` owner;
- raise the UnitArchitecture floor from 787 to 793 and remove only `Agent_Specs.cs` after all six
  ledger rows are terminally mapped.

## Custom pipe-specification cohort

Baseline `4c793038` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Middleware/Authentication_Specs.cs`, both final R0 ledger
obligations, `Pipe.New`, `PipeConfigurator`, its builders, `IPipeSpecification`, validation result
types and failure conversion, and delegate-filter configuration were read before the first test
edit.

The inherited authentication classes are test-owned sample code, not a product authentication
feature. Their durable contract is the public Abstractions extension surface: a custom
`IPipeSpecification<T>` validates before build, applies its filter in configured order, chooses its
test-owned branch, and still invokes the following pipe segment. The greenfield owner is therefore
`ViciOne.ServiceBus.Abstractions.Tests/Middleware/Configuration/PipeSpecificationTests.cs`; the old
Core placement and authentication naming are not copied.

Acceptance checklist:

- replace the authenticated-path obligation with one deterministic ordered trace through a custom
  specification, its allowed branch, and the following pipe segment;
- replace the invalid-setup obligation with one two-row theory that proves both an empty and a null
  role set yield an exact `ConfigurationException` failure result;
- keep the role router entirely test-owned and make no product authentication claim;
- add two passive requirement-projection rows, one for each test method;
- raise the UnitArchitecture floor from 793 to 796 and remove only `Authentication_Specs.cs` after
  both ledger obligations are terminally mapped.

## Bound pipe-context cohort

Baseline `5bd85752` is clean and byte-identical to its private remote branch. The complete inherited
`tests/ViciOne.ServiceBus.Tests/Middleware/Bind_Specs.cs`, its single final R0 ledger obligation,
`UseBind`, both bind configurators, `BindPipeSpecification`, `PipeContextSourceBindFilter`,
`BindContextProxy`, `BindContext`, and both `IPipeContextSource` contracts were read before the
first test edit.

The inherited test has no assertion and waits up to five seconds for a completion source. The
product operation itself is synchronous with respect to the returned task: it awaits the source's
bound pipe and then the following outer segment. A greenfield replacement therefore needs neither
a completion source nor a timeout. Its source owner is the Core configuration surface, and its test
belongs under `ViciOne.ServiceBus.Tests/Configuration`.

Acceptance checklist:

- replace `OBL-R0-CORE-B-0319` with one ordinary xUnit fact;
- prove by exact identity and values that the bound filter receives both the original left context
  and the right context created by the source;
- prove exact order across the configured `ContextPipe`, the bound pipe, and the following outer
  segment;
- use no console output, sleep, timeout, completion source, random input, or wall-clock assertion;
- add one passive Core requirement-projection row and one terminal disposition;
- raise the UnitArchitecture floor from 796 to 797 and delete only `Bind_Specs.cs` after the single
  ledger obligation is terminally mapped.

## Cache bucket and capacity cohort

Baseline `1ce18a5d` is clean and byte-identical to its private remote branch. The complete inherited
`Middleware/Caching/Bucket_Specs.cs`, all seven final R0 obligations, the complete `GreenCache`,
`NodeTracker`, bucket collection/node implementation, statistics, observer, index/factory path,
cache settings and usage-notification contracts were read before the first test edit.

The inherited file mixes one direct `Bucket` invariant with public `GreenCache` capacity and age
behavior. A greenfield replacement splits those source owners into `Caching/Internals/BucketTests`
and `Caching/GreenCacheCapacityTests`. Seven inherited methods become five ordinary xUnit methods
and seven execution cases: two pairs are cohesive parameterized scenarios, not hidden case loss.

Background cache work is synchronized only through real `ICacheValueObserver<T>` events. The
repository's single typed `OperationTimeout` and the current xUnit cancellation token bound a broken
event path; no sleep, polling delay, wall-clock assertion, inherited `TaskUtil`, or arbitrary local
timeout is allowed. `TestCacheSettings.CurrentTime` is fixed to `DateTime.UnixEpoch` before cache
construction and advanced explicitly.

Acceptance checklist:

- prove exact bucket count, head, and node back-link after a push;
- prove that a full cache retains every value when time is frozen and when all values remain within
  maximum age;
- prove that expired values are removed even when capacity alone would allow them;
- prove that simple and usage-aware values above capacity shrink to a non-empty set at or below the
  configured capacity;
- assert observer counts and `GetAll` cardinality in addition to cache statistics;
- map all seven ledger IDs to five methods/seven cases, add five passive Core projection rows, and
  remove only `Bucket_Specs.cs` after closure;
- raise the UnitArchitecture floor from 797 to 804 and reject independent bucket, expiration,
  capacity, observer, scenario, and requirement mutations.

## Node-tracker promotion cohort

Baseline `71128140` is clean and byte-identical to its private remote branch. The complete inherited
`Middleware/Caching/NodeTracker_Specs.cs`, its single final R0 obligation, and the complete
`NodeTracker`, `NodeValueFactory`, `FactoryNode`, `PendingValue`, `BucketNode`, observer, and
statistics path were read before the first test edit.

The inherited fixture proves only that the observer eventually sees a `BucketNode`. The complete
observable contract is stronger: one completed pending factory resolves the temporary
`FactoryNode`, the tracker stores and reports a distinct `BucketNode`, both paths expose the exact
same value instance, and the tracker records exactly one miss, one total addition, and one current
value. A private capturing observer is sufficient; it waits on the actual `ValueAdded` event and
the central typed operation timeout, with no shared mutable test framework.

Acceptance checklist:

- prove temporary-factory resolution and exact value identity;
- prove observer node type, distinct node identity, and exact observer payload;
- prove exact current count, total count, miss, hit, and create-fault statistics;
- add one passive Core projection row and one terminal disposition for `OBL-R0-CORE-B-0336`;
- raise the UnitArchitecture floor from 804 to 805 and delete only `NodeTracker_Specs.cs` after
  focused, unfiltered, and independent one-cause mutation gates pass.

## Cache indexes and pending-value factories cohort

Baseline `ad7bf86b` is clean and byte-identical to its private remote branch. Before the first test
edit, the complete inherited `Middleware/Caching/Tests.cs`, `Index_Specs.cs`,
`MissingValueFactory_Specs.cs`, and `TwoIndex_Specs.cs`, all fourteen final R0 obligations, and the
complete current `GreenCache`, `Index`, `NodeTracker`, `NodeValueFactory`, `FactoryNode`,
`PendingValue`, bucket-node, statistics, settings, and observer boundary were read.

The replacement follows the source owners rather than the inherited fixtures:

- direct cache insertion and coordinated multiple-index behavior belong under `Caching`;
- index lookup/factory behavior and pending-value arbitration belong under `Caching/Internals`;
- the inherited `SimpleValueFactory` success/fault self-tests are not recreated as isolated tests
  of test code. Their exact success and failure shapes are exercised through the corresponding
  product paths and bound to those product tests in the terminal disposition.

All concurrency is test-controlled. A completion source holds the first factory pending until the
second request and concurrent plain read are registered. Multiple-index propagation and removal
wait on real cache-observer events using the central operation timeout and the current xUnit
cancellation token. No sleep, polling delay, current-clock assertion, random input, inherited
fixture, or shared legacy TestFramework helper is allowed.

Acceptance checklist:

- preserve fault propagation and post-fault index cleanup with exact exception contracts;
- preserve successful creation, exact returned values, identity on a later plain read, and exact
  one-call factory accounting;
- prove that a controlled failing first factory permits a second factory to supply one shared exact
  value to the second request and an already-pending plain read;
- prove `NodeValueFactory` fallback, lone-failure, and healthy `PendingValue` identity directly;
- prove direct cache insertion updates its index, never invokes the supplied fallback factory, and
  records one held value;
- prove two-index propagation, clear, post-clear reuse, and removal consistency for the inherited
  100-value boundary, including exact hit/miss/count and visible-value state;
- map all fourteen ledger IDs to twelve ordinary xUnit facts, add twelve passive Core projection
  rows, and give both inherited test-helper-only rows an explicit product-path disposition;
- delete the four fully replaced inherited fixture files; retain or relocate any old support type
  still required by a not-yet-migrated fixture rather than silently breaking that fixture;
- add one native hardening case for truthful removal while a value factory is still pending, raising
  the cohort from 12 inherited-behavior facts to 13 total facts without presenting it as inherited;
- raise the UnitArchitecture floor from 805 to 818 and reject independent factory, cleanup,
  identity, second-index, clear, remove, statistics, visible-value, and requirement-projection
  mutations before closure.

## Internal endpoint-resource cache cohort

Baseline `5a60d423` is clean and byte-identical to its private remote branch. Before the first edit,
the complete inherited `Caching/CacheRecovery_Specs.cs` and `Caching/Cache_Specs.cs`, their thirteen
final R0 obligations, all twenty files under `src/ViciOne.ServiceBus/Internals/Caching`, and every
production call site were read. This cache is not an unused predecessor of `GreenCache`: it remains
the resource owner for core send endpoints and for ActiveMQ, Event Hubs, and Amazon SQS producer or
topology resources. Its behavior therefore remains a product capability until a separate,
fully-proved consolidation slice replaces those consumers.

The inherited files mix duplicate add/read checks, `Task.Delay`, elapsed-time assertions, random
Zipf samples, test-helper self-evidence, and assertion-free stress loops. The native replacement
keeps every meaningful behavior but does not reproduce those mechanisms:

- one exact add/read fact carries the four duplicate simple-add obligations;
- controlled completion sources prove single-flight behavior and both one-failure and two-failure
  recovery without sleeps or scheduler assumptions;
- fixed key sequences prove capacity, usage preference, and hit-ratio accounting without random
  input;
- a direct tracker fact proves high-churn bounded eviction with observable removal counts rather
  than merely completing without an exception;
- a sequential TTL-cache fact preserves the inherited no-expiration distribution boundary;
- two separate `NEW_HARDENING` facts prove invalid TTL configuration and expiration against a
  manual `TimeProvider` whose timestamp frequency differs from `TimeSpan.TicksPerSecond`.

The source inspection found a real unit defect: `TimeToLiveCachePolicy` compares a
`Stopwatch.GetTimestamp()` delta with `TimeSpan.Ticks`. Those quantities use unrelated frequencies
on supported systems. The minimal product correction is to use one injected `TimeProvider` for
timestamp creation and `GetElapsedTime`; production defaults to `TimeProvider.System`, while the
test advances a deterministic provider. This preserves all existing call sites and removes the
wall-clock dependency.

Acceptance checklist:

- map all thirteen ledger IDs to ten ordinary behavior facts under the actual
  `Internals/Caching` source owner, with duplicate inherited rows explicitly sharing a stronger
  carrier rather than duplicating tests;
- add two independent TTL-configuration/frequency hardening facts and twelve passive Core
  requirement rows;
- prove exact exception identity, cache absence after failure, exact value identity, one effective
  concurrent factory, pending-reader recovery, count, hit ratio, capacity, usage preference,
  tracker eviction, and TTL validity boundaries;
- delete both inherited fixtures and their two support files only after their complete obligation
  closure and after the remaining inherited project still builds;
- raise the behavior floor from 818 to 830 and the final UnitArchitecture floor to 831 with the
  independent same-named-project artifact-isolation fact;
- reject independent fault-retention, duplicate-factory, usage-policy, capacity/eviction,
  TTL-frequency, and requirement-projection mutations before closure.

## Send-endpoint cache consumer cohort

Baseline `37f468ad` is clean and byte-identical to its private remote branch. The sole inherited
fixture `SendEndpointCache_Specs.cs`, obligation `OBL-R0-CORE-D-0403`, the complete
`Transports/SendEndpointCache` implementation, its endpoint-provider call chain, and the underlying
endpoint-resource cache were read before the first edit.

The inherited test proves only that two parallel cold resolutions and two parallel warm resolutions
do not throw. The native replacement keeps that exact two-address/two-round boundary through a real
hermetic `InMemoryTestHarness` and strengthens the verdict to exact identity: both warm results must
be the same instances as their cold results, while the two distinct addresses must never share an
endpoint. No mock can replace the real provider/cache composition for this contract.

Acceptance requires one ordinary source-owner fact under `Transports`, one passive requirement row,
one terminal disposition, central timeout and current-test cancellation on every asynchronous
boundary, a focused test, the unfiltered UnitArchitecture profile at a floor of 832, the unchanged
LocalIntegration profile, a zero-warning Release build, and an independent mutation that bypasses
the endpoint cache and is rejected for endpoint-identity loss.

## Raw System.Text.Json object-consumption cohort

Baseline `19e14d90` is clean and byte-identical to its private remote branch. The inherited
`ConsumeJsonObject_Specs.cs`, obligation `OBL-R0-CORE-D-0027`, the System.Text.Json serializer
context, message-type validation, envelope path, and in-memory receive pipeline were read before the
first edit. No Newtonsoft.Json path participates in this capability.

The inherited fixture only waits for a `JsonObject` handler and never inspects what it receives.
The native source-owner fact keeps the real in-memory serialization and dispatch path, sends a local
three-property contract, and asserts the raw object's exact camel-case property set and typed values.
It uses the central operation timeout and current test cancellation and introduces no second
serializer, fixture framework, test double, sleep, or polling loop.

Acceptance requires one passive requirement row, one terminal disposition, a focused run, the
unfiltered UnitArchitecture profile at 833, the unchanged LocalIntegration profile, a zero-warning
Release build, and a one-cause mutation that replaces the projected object with an empty object and
is rejected by the content assertions.

## Serialization-fault cohort

The complete inherited `SerializationFault_Specs.cs` and
`Serialization/DeserializerFault_Specs.cs`, their three final ledger rows, the request-client fault
handler, `GenerateFaultFilter`, `ReceiveFaultEvent`, CopyBodySerializer, the System.Text.Json
envelope reader, and the real in-memory test harness were read before replacement.

The source contains three independent contracts: a consumer-thrown `SerializationException`
returned to a request caller; an unreadable body under an unregistered media type; and a readable
System.Text.Json envelope whose nested Boolean cannot materialize an integer contract member. The
first inherited verdict asserted only the outer exception type, the second already carried useful
fault metadata, and the third was assertion-free. The native replacements retain the complete
observable boundaries, use local contracts, and assert both exact fault identity and non-dispatch.

Acceptance requires one ordinary xUnit fact and one passive requirement row per inherited
obligation, real in-memory endpoints, central timeout and current-test cancellation on every wait,
the three independent false-green mutations recorded in the cohort evidence, an unfiltered floor
of 854, and successful compilation of the remaining inherited project after both old files are
removed. No TestFramework message, NUnit lifecycle, wall clock, sleep, polling, broad exception,
or assertion-free completion is retained.

## Minimal envelope and delayed redelivery

`MinimalBody_Specs.cs` owns one compound obligation. Its raw envelope actually contains only
`message` and `messageType`; the correlation identifier belongs to the payload, not the envelope,
despite the older ledger prose calling it a message id. The inherited test proved deserialization
only indirectly and measured a one-second retry with `DateTime.Now`.

The product already exposes its real in-memory virtual-time owner as `IInMemoryDelayProvider` from
the same DI graph as the bus. The native replacement observes the actual delayed send, asserts its
exact one-hour `SendContext.Delay`, advances virtual time, and verifies the exact payload, media
type, message URN, and redelivery counts on both deliveries. A complete-profile run showed that
`MessageQueue.DeliverWithDelay` could place `Advance` ahead of delay registration because the whole
path began inside `Task.Run`. Starting the handled asynchronous method directly preserves
fire-and-forget delivery while guaranteeing registration order and avoiding unnecessary thread-pool
work. The corrected profile is 855/855 and the old fixture is terminally replaceable.

## Core MessageBody contract closure

`Serialization/MessageBodyLength_Specs.cs` contains 87 inherited obligations but mixes three
product owners. The accepted Abstractions cohort already replaces 51 of them and the accepted
MessagePack cohort replaces six. The remaining 30 belong to the Core assembly or to the original
three-assembly completeness statement. The frozen source has SHA-256
`2c3451a936c4c624f9be5a72e2d1f6232b1fff57db0bbfc4d22abbadb5d063ca`.

The remaining behavior is carried by `MemoryMessageBody`, `NotSupportedMessageBody`,
`SystemTextJsonMessageBody<T>`, `SystemTextJsonObjectMessageBody`,
`SystemTextJsonRawMessageBody<T>`, and `SystemTextJsonMessageSerializer.Options`. Each serializing
body must expose one exact external byte and text representation regardless of which accessor runs
first, return a read-only stream that rejects a real write, and remain unchanged after that rejected
write. The System.Text.Json envelope case uses a fixed explicit envelope so no assertion depends on
generated identifiers, host metadata, or the clock.

The inherited cross-assembly census is closed by three independent owner-local exact-set facts:
the accepted Abstractions census, a new Core census, and a new MessagePack census. This avoids a
cross-module reference in the Core test project while preserving the original complete boundary.
The final source-file disposition composes all 87 IDs explicitly. All focused and complete gates
pass, so the inherited fixture is now deleted.

## System.Text.Json contract shapes

Four remaining fixtures share one product concern and can be verified without retaining their NUnit
or TestFramework structures: constructor-bound immutable messages, extension data, maximum decimal
wire precision, and declared property polymorphism. Their eight ledger obligations become seven
cohesive native cases under the existing `Serialization` source owner.

The inherited `MisnamedProperty_Specs` explanation is factually wrong: its constructor parameters
match the property names case-insensitively. Its useful contract is the real request/response round
trip of getter-only values. The native case awaits the response task and asserts all three response
properties, including the previously ignored `Cost` value.

Extension-data configuration replaces the serializer's process-global options. The two cases must
therefore run outside parallel collections, retain the original options identity, and restore it
even if bus shutdown fails. Both envelope and raw modes assert the exact content type, key set,
`JsonElement` kinds, and values. The polymorphism cases use the standard
`JsonDerivedTypeAttribute` and preserve exact concrete values and collection order without accepting
body-controlled legacy type metadata. The decimal case parses the emitted document and proves one
camel-case property with a quoted lossless maximum value, then reads the exact wire form back.

## System.Text.Json application-format compatibility

Baseline `5f513c7d` is clean and byte-identical to its private remote branch. The complete inherited
Protobuf and XML fixtures, their seven executable ledger rows, the cohort-wide serializer-format
question, the generated Protobuf support files, and both retained System.Text.Json serializer paths
were read before the first edit. Static source-to-test pairing was also executed once with the
Microsoft-skill-provided Roslyn analyzer: it reports both System.Text.Json serializers as paired,
but this parse-only result is only a navigation aid and says nothing about behavioral depth.

`PO-2026-08-16-01` fixes the architectural boundary. XML and raw XML are application values carried
as exact `string` and UTF-8 `byte[]` members; they are not bus wire formats and must never advertise
an XML content type. The removed XML/Raw-XML product serializers remain removed. Protobuf likewise
remains test-only input to the retained System.Text.Json serializers, not a product dependency or a
third wire serializer.

The old trade-domain fixture is much larger than the behavior it proves and checks only a repeated
element count. The Greenfield replacement uses a minimal test-owned `.proto` schema generated at
build time. One generated message deliberately combines a scalar, getter-only `RepeatedField<T>`,
and the generated `Timestamp` well-known type. A probe also tested the broader R0 question's map
idea: System.Text.Json recognizes `MapField<TKey,TValue>` through a converter that supports neither
populate handling nor a replacement value for the generated getter-only property. A test-only full
Protobuf converter would prove itself instead of the product path, so it is rejected. The governing
PO decision requires the real getter-only `RepeatedField<T>` boundary, not map support. A local
partial type annotation therefore remains the smallest standard solution and does not mutate the
product's process-global options. Both the envelope and raw JSON serializers must restore every
required value exactly. The current stable build-time packages selected from their official NuGet records are
Google.Protobuf 3.36.0 and Grpc.Tools 2.83.0; both stay inside the native test tree, with Grpc.Tools
private to the test build.

The three MessagePack XML identities are already terminally replaced by the accepted MessagePack
cohort. This cohort owns only the three System.Text.Json identities plus the Protobuf identity. The
former R0 question `OBL-R0-CORE-C-0473` is resolved by the PO decision: envelope/raw JSON coverage is
added for the generated Protobuf object and for opaque XML text/bytes, while no raw-XML wire
serializer or XML media type is introduced.

## System.Text.Json collection compatibility

Baseline `952a1fa6` is clean and byte-identical to its private remote branch. The complete inherited
`Serialization/MoreSerialization_Specs.cs`, its `SerializationTest` base, all 34 ledger rows, the
existing native collection tests, requirement projection, and the complete envelope serializer path
were read before the first edit. The Microsoft-skill Roslyn pairing analyzer was run once as a
navigation aid; its parse-only pairing is not treated as behavioral coverage.

The inherited file executes the same 17 scenarios once for MessagePack and once for System.Text.Json.
All 17 MessagePack identities are already terminal in the accepted MessagePack disposition. The only
open work is therefore the 17 interleaved Core identities `OBL-R0-CORE-C-0297` through `-0329`.
They belong to one hermetic UnitArchitecture owner and exercise the retained System.Text.Json
envelope path. No product change, additional serializer, test framework, clock, network, or inherited
fixture hierarchy is required.

The old equality implementations hide which member failed and one case embeds the current time.
The Greenfield replacements use minimal test-owned contracts, fixed values, and member-level oracles.
They preserve ordered nested object lists; empty, singleton and multi-entry dictionaries; concrete
and interface sets; nested objects; empty, singleton and multi-element primitive arrays; generic
object arrays; private setters; read-only dictionaries of read-only lists; empty contracts; enums;
and ordered key/value pairs. The duplicate inherited empty-array identity remains explicitly mapped
to the same stronger native carrier rather than creating a duplicate test.

Two carriers already exist and are stronger than the inherited cases. The scalar test exercises a
constructor-bound type with a private setter and a control character, and the collection test
preserves an ordered key/value list even when keys repeat. Their methods are reused by the terminal
disposition; duplicating either test would add maintenance without adding a distinguishable product
verdict.

## Core serializer configuration and forwarding

Baseline `cc6f2e11` is clean and byte-identical to its private remote branch. The complete inherited
`Serialization/SerializationConfigurationValidation_Specs.cs` and
`Serialization/Forward_Specs.cs`, all 16 ledger obligations, the Core
`SerializationConfiguration`, both JSON-options extension methods, `ForwardMessagePipe<T>`, the
public forwarding extensions, the current native requirement projection, and the in-memory test
harness usage were read before the first edit.

The configuration fixture owns fourteen independent boundaries. Six validate registered,
unregistered, case-insensitive, and inherited media types. Three prove that per-message options use
the callback result for both mutate-and-return and replace-instance forms and reject a null result.
Five prove that process-global options receive a defensive copy, retain either returned form,
reject null without losing the previous instance, and remain untouched when no callback exists.
The global-options cases must run in the existing non-parallel collection and restore the original
instance unconditionally.

The forwarding fixture contains the same System.Text.Json behavior twice. One stronger native fact
is therefore the single execution owner for both obligations: a concrete message is consumed via an
interface, forwarded through the real in-memory pipeline, and observed again as the concrete type.
It must preserve fixed message values and identifiers, an explicit custom header, the additional
concrete-only property, the original JSON content type, and exactly one downstream delivery. No
clock, sleep, polling, inherited TestFramework type, or duplicate test is retained.

The complete forwarding path shows two legitimate preservation layers rather than one redundant
mechanism. `ForwardMessagePipe<T>` first populates the outgoing send context for transports,
observers, middleware and the optional caller pipe. The selected serializer then clones the original
envelope so interface consumption does not lose concrete payload members or supported type identity,
and applies final send-context overrides when the body is materialized. The replacement observes
both layers independently. The same path review exposed a null child-pipe probe and an inverted
`PipeExtensions.IsEmpty` branch; both are unambiguous product defects and are corrected with native
regression coverage. Serializer-specific envelope-update duplication and deterministic expiration
remain an independent product-normalization slice in the root `TODO.md`, not a hidden test workaround.

The subsequent full-path review resolved the previously open forwarding-expiration policy. An
expired generic forward runs its caller pipe first and proceeds only when that pipe leaves a positive
TTL. Otherwise one internal forwarding marker is consumed before normal transport dispatch, before
persistent-outbox storage, or before mediator dispatch, and emits one structured
`FORWARD-EXPIRED` event without retry or fault. Responses and faults retain their separate one-second
minimum. System.Text.Json, MessagePack, replacement-message, outbox, mediator, positive/null override
and structured-log tests cover the decision; five targeted mutants each fail at their intended
boundary.

## Envelope metadata and contextual time

The complete constructors and update paths of `JsonMessageEnvelope` and `MessagePackEnvelope` were
compared field by field. They duplicated the same transport metadata and both read
`DateTime.UtcNow`; their update paths also converted every nonpositive TTL into one second. The
MassTransit 8.5.10 upstream contains the same implementation but no source-level rationale for
making that a serializer rule. It conflicts with the separately reconstructed forwarding policy:
only response/fault creation owns the one-second exception.

The Greenfield boundary is therefore one internal value projection in Abstractions, consumed by
both retained serializer assemblies through signed internal visibility. No public metadata model or
product package was added. The existing public `PipeContext` payload mechanism carries the standard
.NET `TimeProvider`, exposed through a small extension API; tests use the official
`Microsoft.Extensions.TimeProvider.Testing` package only. The projector copies or merges headers
case-insensitively, preserves existing optional overlay fields, intentionally replaces the final
destination as before, and captures one UTC snapshot whenever sent time or relative expiration
needs it. Message payload encoding and MessagePack's defensive byte-copy behavior remain untouched.

Before implementation, the target tests failed for ignored contextual time, divergent format
expiration and the hidden one-second clamp. After implementation all three complete project suites
pass. Four real mutations independently proved that the tests reject a system-clock substitution,
reintroduced clamp, second clock read and one-field MessagePack drift.

## Testing observation primitives

The complete inherited `Testing/AsyncMessageList_Specs.cs` and
`Testing/InactivityObserver_Specs.cs`, their thirteen ledger rows, the public asynchronous-list
surface, every concrete list constructor, both observer implementations, the two harness
composition paths, and the adjacent saga-polling paths were read before the first edit. The original
MassTransit 8.5.10 source has the same process-clock and timer implementation; its repository history
contains no rationale that makes those mechanisms part of the behavioral contract.

The immediate owner is smaller than the complete harness timing surface. `AsyncElementList<T>` owns
all nine list obligations, while `AsyncInactivityObserver` owns four interval obligations. The
remaining harness-budget, rolling-timer, saga polling, and recorded-message timestamp paths are
distinct source owners and remain explicitly deferred; they are not silently claimed by this
cohort.

## Test-local message-group sample

The complete inherited `Groups/Group_Specs.cs` and all three ledger rows were read. Its extension,
collection, marker interface and correlated message types are declared locally; retained source,
tests, samples and benchmarks contain no reference to them. Product `GroupKeyProvider` types belong
to batch grouping and share only the ordinary word “group.” Recreating these cases would introduce
new test-only production logic into the test suite, so the correct disposition is terminal
non-product removal.

Both immediate primitives currently hide the process clock. The asynchronous list creates a real
timer, the synchronous list combines `DateTime.Now` with timed `Monitor.Wait` and holds its monitor
across yielded values, and the inactivity observer uses real `Task.Delay`. A Greenfield replacement
uses the standard .NET `TimeProvider`, defaults existing public construction to
`TimeProvider.System`, and provides explicit provider-aware overloads. One provider owns each
observation. The synchronous iterator uses the same provider-backed cancellation deadline and
copies pending elements outside its monitor, so it neither blocks producers across consumer code nor
contains a second wall-clock algorithm.

The native tests use `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider`, never sleep and
never use elapsed wall time as an oracle. They preserve existing/immediate selection, include,
exclude, pattern, later arrival, cancellation and timeout behavior; the observer cases preserve both
force orderings, active-to-inactive transition and exactly one query per virtual interval. Additional
hardening covers the synchronous deadline and makes observation-source failures visible instead of
turning them into a permanently pending false-green state.

## Testing harnesses, consumers, handlers and sagas

The nine remaining inherited Testing fixtures contain 42 R0 obligations. They are preservation
evidence only. A static Roslyn source-to-test pairing scan was run once before implementation and
reported 3,912 product sources, 1,164 test sources, 657 heuristic pairs and 3,255 unpaired sources.
Those figures are a navigation aid, not coverage or completeness evidence. The complete nine
fixtures and their consumer, handler, saga, state-machine, in-memory bus, dependency-injection,
observer, message-list and timeout implementation paths were read before the first edit.

The product has two legitimate public test entry points. `InMemoryTestHarness` is a lightweight
direct composition surface; `AddViciOneServiceBusTestHarness` is the dependency-injection surface
with hosted-service lifecycle and registered consumer/saga harnesses. They are not interchangeable,
but must share one timeout, inactivity, observation, message-timestamp and saga-polling model. Each
test owns and disposes exactly one harness; the old NUnit fixture-reset method is compatibility debt
until the separate TestFramework retirement and is not used by native tests.

The path review found hidden process time in harness cancellation, rolling timers, message-record
timestamps and saga polling. It also found that `HandlerTestHarness` records a handler exception but
does not rethrow it, incorrectly allowing the consume pipeline to report success. The A+ boundary is
one standard .NET `TimeProvider` per harness, propagated to all lists, observers, timers and polling
helpers; existing public construction remains source-compatible and defaults to
`TimeProvider.System`. Handler faults are recorded and rethrown. Tests use deterministic fake time,
coordination primitives and real in-memory dispatch, never sleeps, stopwatch thresholds or polling.

Coverage is derived from product behavior rather than the old test count. In addition to terminally
mapping all 42 R0 obligations, the native set must cover invalid construction, timeout and
cancellation boundaries, start/stop/dispose lifecycle, DI override propagation, destination and
response addressing, multiple contracts, exact observations, consumer and handler faults,
unsupported saga repositories, saga existence/nonexistence/matching, state-machine state and fault
transitions, recorded timestamps and independent harness isolation.

### Observation-list and observer gap review

The complete public `SentMessageList`, `PublishedMessageList`, `ReceivedMessageList` and typed
`ReceivedMessageList<T>` surfaces were compared with their common `AsyncElementList<T>` owner and
with the send, publish, receive and receive-endpoint observers that populate them. The static
source-to-test pairing report is only navigation evidence: indirect harness use makes several files
look unpaired even when a behavior test reaches them. The source review therefore enumerates the
observable contract directly instead of treating filename pairing as coverage.

The inherited tests cover isolated list and observer success paths but do not prove all public query
shapes, consistent include/exclude behavior across all three message kinds, exact fault recording,
duplicate and missing identifier handling, filter-failure propagation, empty-sequence extension
semantics or the typed received-list facade. The native replacement must cover those boundaries with
real message contexts and the real in-memory harness where integration matters. No wall clock,
sleep, polling or shared running harness is permitted.

`TestReceiveEndpointObserver` also accepted a null publish observer and deferred failure until an
endpoint became ready. This is an unambiguous public construction defect. The A+ behavior rejects
the missing mandatory dependency immediately with `ArgumentNullException` naming
`publishObserver`; tests do not preserve the delayed null-reference failure.

The observer's `Ready` connection is not redundant with the bus-level publish observer. Its source
and local history show that it is the unchanged upstream mechanism for endpoints added after bus
startup. A real dynamic in-memory endpoint was therefore connected after the harness started and
published from its consume context into a separately owned observer connected only through
`TestReceiveEndpointObserver`. Removing the `Ready` connection made exactly that test fail with an
empty observation after the publication itself had completed. The path is retained as a justified
endpoint-lifecycle boundary, not as unexplained compatibility code.

### DI testing utilities and dynamic endpoints

The complete `TestingServiceProviderExtensions`, `ServiceProviderTestExtensions`,
`InMemoryTestHarnessBusInstance`, its registration factory, both receive-endpoint connector
interfaces, consumer decorator/registration chain, container harness and all current native DI
tests were read together with the two deleted inherited DI fixtures. The inherited tests prove only
basic harness, consumer, saga and state-machine success. They do not prove filtered dynamic publish
handlers, registered task identity/order, either registration-aware endpoint-connector overload or
their failure boundaries.

The in-memory bus-instance implementation is composition behind `IReceiveEndpointConnector`; its
implementation type should not become a test-owned public contract. Both overloads are therefore
tested through the resolved interface and real endpoints. `ConnectPublishHandler<T>` currently
accepts missing required arguments and awaits endpoint readiness without the harness time budget.
The Greenfield boundary is an immediate argument failure plus the existing standard harness
`TimeProvider`, timeout and cancellation token; no new timer abstraction or wall-clock wait is
introduced.

The dynamic-endpoint execution uncovered an upstream inconsistency that the inherited suite never
exercised. `AddConfigureEndpointsCallback` promises invocation for each receive endpoint, and the
default registration bus, dedicated in-memory harness bus and transport-typed connector already
honored that contract. The general connector overloads on `TransportBusInstance` alone skipped it.
The normal transport path now applies the same collected callbacks before the caller-specific
configuration. Both callback overloads are observed exactly once for queue-name and endpoint-
definition creation, including the real registration context.

The first parallel namespace run also exposed a test race rather than a product fault. A terminal
fault publication can precede `BusTestConsumeObserver.ConsumeFault`; snapshotting the consume list
immediately after only the publication is therefore invalid. The corrected test subscribes to both
independent terminal observations before publishing and snapshots only after both complete. Two
subsequent complete 105-case Testing-namespace runs pass without failure or skip.

## Observer pipelines and message-flow diagnostics

The complete inherited `MessageFlow_Specs.cs`, `Observer_Specs.cs`, `PublishObserver_Specs.cs`,
`ReceiveObserver_Specs.cs`, and `SendObserver_Specs.cs` plus all connected observer, observable,
transport, mediator, consume-filter, connection-set and timeline sources were read before closure.
The old set contains 27 useful historical intents, but many cases only await one callback. It does
not specify order, identity, mutually exclusive terminal stages, disconnection, fan-out failure,
observer-branch isolation or the full rendered topology.

The source establishes three distinct receive levels: transport delivery, individual consumer, and
endpoint error handling. A middleware failure after successful consumption therefore raises
`ReceiveFault`; when the endpoint error pipe handles it, the delivery subsequently completes with
`PostReceive`. A complete parallel run exposed that distinction after an isolated run had made an
incorrect three-stage assertion appear plausible. Delivery assertions now correlate the exact
`ReceiveContext` and bind the four-stage handled-fault sequence.

`IObserver<ConsumeContext<T>>` is one consume branch, not a filter over sibling handlers. Its
failure is reported unchanged to `OnError` and as that branch's consume fault while another handler
continues. Publish notifications are adapted through transport sends but remain publish-only to
public observers. Mediator request/response notifications are nested around the response. The
timeline topology is a deterministic 1/3/3/3/9 graph and is rendered immediately with fake time.

The only product defect in the bounded path was the mutable array returned by `Connectable<T>` and
its incomplete handling of synchronous/null-task asynchronous fan-out failures. The fix preserves
the internal cached snapshot for constrained-device performance and copies only at the public array
boundary. Every other product path is preserved and now has stronger executable contracts.

## Message identity, conversation, time, and headers

The complete five inherited fixtures and their connected topology, send-endpoint, consume-transfer,
message-context, header-container, JSON-object, and dynamic-interface paths were read together. The
old cases are a lower bound: they cover positive convention choices and basic metadata presence but
not precedence, overrides, empty identifiers, exact causation, exact time, storage ownership, or
complete object-header values.

Correlation is a stable message-type topology decision. Its order is explicit selector,
`CorrelatedBy<Guid>`, `CorrelationId`, `EventId`, and `CommandId`; the caller's send pipe may override
that result afterward. Conversation identity is generated at the final endpoint boundary, inherited
from a consume context when present, and deliberately replaced with an initiating-conversation
header when requested. Source address has two compatible safeguards: exact consume-endpoint transfer
and a null-only contextual endpoint fallback.

The review found a real inherited defect in `DictionarySendHeaders`: the independent-copy branch
constructed a populated dictionary and then added the same entries again. The corrected branch is
one case-insensitive snapshot; the explicitly requested shared dictionary remains shared. A separate
JSON test uses caller-owned options without the global converter because the real transport path has
two valid interface-materialization routes and would otherwise mask loss of the direct fallback.

Seventeen native facts, exact 15/15 ledger disposition, eleven killing product mutations, the
550-case Core run, the unfiltered 1080-case UnitArchitecture profile, and the unchanged 3-case
LocalIntegration profile close the cohort without skips or wall-clock assertions.

## Message contexts and dynamic contracts

The four inherited context/proxy fixtures, all 30 ledger obligations, and the complete connected
request client, response handlers, send endpoint, initializer, reflection emitter, System.Text.Json,
raw JSON and MessagePack paths were read as one behavior boundary. The inherited assertions were a
lower bound: they omitted most identities and addresses, accepted response types, independent
subscriber delivery, timeout identity, caller-token preservation, nested anonymous values,
unsupported contract shapes, concurrency, collectibility and attribute fidelity.

The emitted message type is a property-only data contract. Closed interfaces and all inherited
properties are accepted; behavior members, static/default/indexed/write-only shapes and ambiguous
case-insensitive wire names fail before emission. Compatible inherited duplicates are merged
deterministically. Constructor and named attribute arrays must be converted to their declared CLR
array type. The latter exposed an existing `InvalidCastException` and is fixed at the shared custom-
attribute conversion boundary. Request cancellation exposed a second product defect: the response
task used an anonymous cancellation token rather than the caller's exact token. Invalid proxy shapes
previously leaked late `TypeLoadException`/`InvalidOperationException`; they now fail as precise
argument errors.

The request deadline still used `System.Threading.Timer` directly. That made its correctness test
depend on real elapsed time even though the platform already standardizes testable time sources.
`ClientFactoryContext` now owns a `TimeProvider` with a source-compatible system default; all built-in
contexts accept or forward it, and the request handle creates its deadline `ITimer` there. The native
test waits for deterministic timer registration, advances virtual time, and asserts the exact timeout
without sleeping.

The resulting 24 new Core cases plus the existing MessagePack interface case terminally cover all
30 inherited rows and add the missing source-derived contracts. Eight one-cause mutations prove the
new boundaries. Core passes 574/574 and the unfiltered UnitArchitecture profile passes 1104/1104,
with no failure or skip.

## Request clients, response matching, mediator, and multibus

The six inherited request fixtures and all 40 connected ledger identities were read as migration
evidence, not as an API specification. The complete client-factory, request-handle, response,
mediator, outbox, filter, TTL, dependency-injection, and multi-bus paths establish the retained
capabilities. MassTransit source/binary compatibility is not required: useful behavior remains,
while signatures and ownership may change to produce a coherent greenfield ViciOne.ServiceBus API.

Source analysis exposed five owner-level corrections: deadline and transport TTL were coupled;
deadline and response/fault expiration bypassed the context clock; several public construction
paths leaked late null failures; response wrappers did not enforce complete deterministic context
behavior; and outbox bypass was repeated in four concrete request endpoints instead of being an
invariant of the shared base. Each correction is implemented at its product owner and protected by
direct behavior plus a one-cause mutation. The inherited nested-outbox scenario alone did not detect
loss of bypass, so a transport-backed direct test now proves the invariant independently.

All 40 inherited identities are mapped exactly once to 24 permanent test methods. The six inherited
files are removed, no empty test directory remains, and the additional source-derived cases cover
the omitted three-response, race, null-boundary, accepted-type, TTL, outbox, filter, mediator-time,
and secondary-bus boundaries. Core passes 614/614, Abstractions 150/150, UnitArchitecture 1155/1155,
and LocalIntegration 3/3 with no failure or skip. Release builds have zero warnings and errors.

## Accepted cohort — type relationships and readable-property reflection

The four inherited type-extension/property fixtures contribute 37 obligations, but they are only a
lower bound. Several assertions are duplicates or counts, and their `GetClosingArguments` calls bind
to a test-local copy of the old helper rather than reliably exercising the product implementation.
The complete product helpers, cache, consumers and existing native property tests were therefore
read before the first edit.

The current API mixes three different questions: whether any closed generic match exists, which
single match a caller requires, and which arguments that match carries. It silently selects the
first interface when a type closes the same generic interface more than once. Its cache also exposes
an implementation type publicly and attempts to store negative reference values. The A+ contract
separates any-match, complete deterministic match enumeration and exactly-one selection; ambiguous
single-match requests fail explicitly. Historical method names and overloads are not retained merely
for MassTransit compatibility.

Readable instance and static property discovery is also named explicitly. Each property is selected
by its own accessor instead of by the shared `get_` method name, inherited interfaces are traversed
deterministically before the derived interface, and diamond inheritance is de-duplicated. This keeps
the existing derived-property-wins consumers correct and prevents one readable indexer from making a
different write-only indexer appear readable.

The connected caller review exposed an independent registration defect: runtime future registration
indexes an empty generic-argument array when the supplied type is not a state machine and accepts a
state machine over the wrong state type until reflection fails later. The product boundary must
reject both shapes directly with the stable `futureType` argument contract.

The completed review also found that strong type-keyed dictionaries in the generic relationship,
short-name and formatter caches would prevent unload of collectible assemblies. All three use weak
type-key ownership and one collectible-assembly fact kills each strong-cache regression. A reversed
ambiguous-interface declaration proves that deterministic result order comes from the product sort,
not incidental reflection order. The four inherited fixtures are terminally replaced; their
test-local helper remains temporarily because unrelated inherited fixtures still compile against
it. A broader non-public read/write-property policy question is explicitly deferred in `TODO.md`
instead of being hidden in a test workaround.

## Middleware coordination and resilience

The eleven inherited middleware fixtures and the complete connected product implementations were
read as one composition boundary. The old tests establish 19 useful identities but omit repeated
configuration changes, rollback after cancellation, null runtime keys, concurrent state ownership,
resource disposal and deterministic time. Those boundaries are derived from the product rather than
copied from fixture structure.

The product review found that one-time setup retained a failed terminal state, dynamic rate and
concurrency changes used the initial rather than current limit, cancelled decreases leaked acquired
permits, runtime partition keys could become an unintended empty route, and latest-value visibility
had no cross-thread memory contract. Rate and circuit timers also bypassed the standard
`TimeProvider`. Each defect is corrected at its owner and has an exact behavior test.

That migration cohort deliberately retained the inherited circuit API and its strict activation
boundary. It is historical evidence, not the current product contract. PO-2026-08-25-04 supersedes
it with a validated `CircuitBreakerOptions` API, an immutable settings snapshot and a timer-free
compare-and-swap state machine. Closed sampling is lazy and uses the configured `TimeProvider`;
minimum throughput and failure ratio are inclusive. After an open duration expires, exactly one
caller atomically owns the half-open probe and every competitor is rejected without reaching the
protected pipe. Probe success closes, a classified failure reopens with bounded backoff, and caller
cancellation or an unclassified failure releases ownership while remaining half-open.

The final boundary additionally freezes exception-filter configuration at pipe construction through
the single shared Abstractions exception-matching owner: retaining the configurator or mutating a
caller-owned type array cannot alter the published filter, and Retry, Rescue, Redelivery, Kill
Switch and Circuit Breaker cannot drift into separate type or aggregate-exception semantics.
Cancellation ownership depends on the caller token actually being requested rather than token
identity alone. A throwing classifier releases an acquired probe before propagating the original
classifier failure. Metrics and activities remain OpenTelemetry-only, carry exact low-cardinality
tags for both open and probe-in-progress rejections and are a no-throw observation boundary, so a hostile in-process listener cannot change
delivery failures or circuit state. The concurrency composition test now holds two real concurrent
entries and proves that a third waits; the recovery race synchronizes 33 callers on the same
Open-to-Half-open transition and admits exactly one.

A complete profile exposed two independent test defects outside the inherited lower bound. Cache
capacity is a documented soft bound of `Capacity + BucketSize`, while `NodeTracker.Cleanup` really
could lose a concurrent cleanup request by resetting its scheduling flag outside the protecting
lock. The oracle was corrected to the documented bound and the product race was fixed under the
same lock. The abandoned-fault test no longer assumes a `WeakReference` dies after five collections;
it proves the runtime observation boundary with a marked unobserved control failure and rejects an
exact mutation that removes the product observer.

All 19 identities are terminally mapped, the eleven inherited files are removed, and seven
effective mutations fail for their intended reason. Final post-deletion results are Abstractions
201/201, Core 643/643, UnitArchitecture 1235/1235 and LocalIntegration 3/3 with no failure or skip;
all affected Release builds have zero warnings and errors.

## Middleware routing, limits and scope

The five inherited fixtures carry eight useful identities but leave the actual product boundaries
largely untested. The complete scope/payload, dispatch/output/tee, dynamic/key routing, limiter and
circuit paths establish the stronger contract. The old wall-clock concurrency loops are replaced by
explicit entry/release gates; the circuit uses virtual time and exact event/exception identity.

`ScopePipeContext` intentionally overlays its own payload cache on a readable parent. An inherited
payload returned by `GetOrAddPayload` is not copied, while `AddOrUpdatePayload` creates a local
replacement and leaves the parent unchanged. Both protected constructors now reject a null parent
immediately, and nested scopes preserve nearest-readable/local-write isolation.

Dynamic dispatch is compatible-type fan-out. Every compatible output pipe completes before the
original input continuation runs exactly once; nonmatching outputs do not suppress it. Keyed routing
adds an independent exact-key condition. A connected output type must implement the input context
because the existing continuation contract is typed that way; this real boundary now fails with a
stable product diagnostic instead of a reflective generic-constraint error. Generalizing the API to
unrelated output contexts would be a separate architecture change, not an accidental test demand.

Source review exposed missing null/result guards for parent contexts, converter factories, key
accessors, converter instances, successful-null conversions and configured/runtime keys. All are
fixed at their owners. The then-established strict circuit activation rule and router event were
covered as migration evidence. Both have since been superseded by PO-2026-08-25-04: the greenfield
breaker uses inclusive ratio/throughput boundaries, a distinct rejection exception and
OpenTelemetry-only transitions.

All eight inherited rows are terminally mapped, five fixtures are removed, and eight one-cause
mutations fail for their intended reasons. Final results are Abstractions 203/203, Core 649/649,
UnitArchitecture 1243/1243 and LocalIntegration 3/3 without failure or skip; the final Engineering
build and the post-deletion inherited Core build have zero warnings and errors.

## Middleware retry

The inherited retry fixture defines 17 useful behavior identities but relies on command-specific
replacement-context helpers and omits cancellation identity, deterministic time, invalid policy
results, immutable schedules and the general task retry executor. The complete product path shows
that retry ownership is a non-generic payload concern: typed dispatch may change the concrete pipe
context while the retry context retains the observer type. A downstream retry therefore owns the
failure and budget; outer layers may report the terminal nested context but must not retry it.

Source review found recursive retry loops, wall-clock delays, source cancellation rewritten as a
linked token, policy cancellation capable of returning success, cancellation ineffective before
the first failure, mutable caller-owned interval schedules, uncaught schedule overflow, repeated
jitter selection and duplicated task/result loops that could rethrow the wrong failure. These are
product defects, not compatibility behavior, and are corrected at the owning sources.

`MessageRetryPolicyExtensions` has no product or retained legacy-suite caller. It is an incomplete
third execution path beside the configured message-retry middleware and the general task executor;
removing it eliminates duplication without removing a capability. All 17 inherited identities are
terminally mapped, 54 native cases execute, the eight exclusive inherited files are removed and
nine independent mutations reject regression. Final results are Abstractions 203/203, Core 703/703,
UnitArchitecture 1297/1297 and LocalIntegration 3/3 without failure or skip; Release and bounded
format gates are clean.

## Message and host retry integration

The remaining host-retry fixture is not another retry-engine implementation. It is the boundary
that combines a transport host policy with caller cancellation and transport shutdown. Upstream
history shows the fixed one-second pause entered together with the first host send retry loop and
survived later cancellation and transport changes. All retained host configurations already own an
explicit exponential minimum delay, so the additional pause duplicates policy ownership and makes
the actual delay neither configurable nor deterministic.

The remaining message-retry fixtures exercise configuration placement rather than policy
arithmetic: endpoint versus bus ownership, consumer scope, disjoint policies, no/default policy,
bus shutdown and a concrete message whose topology includes base and interface contracts. The old
TypeCast test never raised a failure and therefore never exercised retry. The replacement retains
its abstract-ancestor topology regression while also dispatching through a concrete base and a
public interface, failing once in each pipeline and proving exact recovery.

The separate receive-transport agent contains another inherited reconnect loop and fixed breather.
It is causally different from host send retry and has a larger supervisor/ready/fault lifecycle;
changing it in this bounded cohort would be guesswork. It is recorded as its own path-complete
product slice in `TODO.md`.

## Configuration composition and validation

The eight inherited fixtures contribute 29 useful behavior identities but mix configuration
observation, send ownership, saga reflection, runtime instances and invalid-startup behavior. The
complete connected product paths were read before replacement. The source-derived review added six
missing boundaries: late root send/publish configuration must reach already-created message
specifications; failed send/publish observer initialization must not poison the cache; invalid saga
message contracts must be ignored; and an unsupported saga construction shape must fail with one
actionable configuration error.

Observer callbacks may mutate configurators before failing, so automatic retry cannot be made
transactional. The A+ lifecycle is exactly-once notification with re-entry rejection and a sticky,
identity-preserving first failure. Send and publish roots publish a provisional message entry before
recursive discovery for safe same-message re-entry, but remove that entry if initialization fails.
Validation results are materialized snapshots rather than live enumeration over mutable owners.

Saga reflection order is not a contract. Discovery uses semantic role precedence and ordinal
message identity, filters invalid message types, keeps the first role for duplicates, and compiles
one synchronous construction delegate for either a public `Guid` constructor or a public
parameterless constructor with writable `CorrelationId`. The former public interface and role DTO
were implementation remnants rather than useful capabilities and are removed.

All 29 inherited identities close against ordinary source-owner tests. Ten one-cause mutations cover
snapshot materialization, parent composition, deterministic saga discovery, role precedence,
observer failure ownership, observer-injected validation, late root propagation and failed-cache
recovery. Static assertion, gap and anti-pattern review found no remaining defect in the bounded
cohort.

## Pipe-context failure precedence

The complete supervisor, context factory/handle, active-context agent and base lifecycle path shows
one authoritative operation result. A cleanup exception after a successful send is not evidence
that the send failed and must not invite a duplicate. After an operation failure, fault
notification, stop and disposal remain cleanup; none may replace the exact causal exception, and all
stages must still be attempted.

The inherited fixture covered five useful outcomes but not exact exception identity, complete event
order, caller cancellation-token propagation or a failure while awaiting the context itself. The
replacement adds those boundaries with deterministic in-memory fakes and no timing or transport
dependency. Existing cache tests independently retain invalidation and recreation behavior.

Eight isolated mutations remove or corrupt operation execution, fault notification, stop,
disposal, exception precedence and token propagation. Every mutation is rejected by its owning
test. The product behavior was already correct; the only product edit removes incident-specific
commentary and uses transport-neutral diagnostics.

## Timeout and cancellation

The inherited timeout implementation was still the upstream v8.5.10 shape: a linked cancellation
source, `CancelAfter` on system time and a final timeout exception without the original cause. The
old tests used wall-clock waits and covered only timeout short-circuit, one shutdown case and one
independent cancellation case. They did not distinguish caller, deadline and transport ownership or
exercise the configuration graph.

The complete path shows that fault generation occurs inside the timeout consume context before the
outer filter can classify the cancellation. Timeout classification therefore belongs at both
boundaries: the context projects it into `Fault<T>`, while the outer filter reports it to the caller.
Transport cancellation is owned by the original context and suppresses application faults.

The standard `TimeProvider` boundary removes wall-clock dependence. A context provider is the normal
single time source; an explicit provider is a supported override and must be captured with the
timeout duration when the pipeline is built. Retaining and later mutating an internal configurator
must not change an already compiled pipe. Timer disposal needs an observable provider that tracks
the actual `ITimer`, not merely the linked token; the common `ObservableTimeProvider` now owns that
capability and replaces a duplicate Circuit-Breaker-only helper.

Mutation work found two initially false-positive assurances. A disposed linked source hid a leaked
deadline timer, and a shutdown test without installed timeout middleware could not protect timeout
fault suppression. Both tests were strengthened against the real product boundary before the
cohort was accepted. The final twelve effective mutations each fail for their intended cause.

## In-memory delay and scheduled publish

The imported v8.5.10 provider combined process UTC, a mutable offset, an unbounded operation channel,
a permanent reader task and an invalid duplicate-key comparer. Its four tests measured one-second
wall-clock windows and did not establish the ordering promised by their names. A canceled far-future
entry also remained retained until its deadline, while `Advance` acknowledged queue admission rather
than application of the time change.

The complete host/fabric/queue/DI path needs one domain behavior surface for in-memory delayed
delivery, but not a custom clock abstraction. The final provider therefore accepts standard .NET
`TimeProvider`, owns one timer, orders `DateTimeOffset` deadlines by a stable sequence and applies
manual advancement synchronously. The normal production constructor uses `TimeProvider.System`.

The scheduled-publish behavior test initially did not always reject a reintroduced `Task.Run`,
because the thread pool sometimes won the race. A second ordinary assurance now reads the compiled
async state machine, proves the direct `DeliverWithDelay` call and rejects `Task.Run`. A real mutant
proves the IL reader is not vacuous. The final 18-case cohort contains no sleep or wall-clock oracle.

## Bus health waiting

`BusControlHealthExtensions` is the single public owner for waiting until one or more bus controls
reach an expected health state. The imported API polls `DateTime.UtcNow`, sleeps for a fixed 100 ms,
returns only the final enum and silently returns an unexpected status when its timeout expires. Its
async methods also lack the `Async` suffix, do not validate their public arguments and cannot be
driven deterministically. The cancellation overload is used by the EF Core outbox delivery service;
the timeout overloads are also used by inherited broker and kill-switch fixtures.

The A+ boundary keeps polling as an implementation detail but injects the standard .NET
`TimeProvider`, defaults production calls to `TimeProvider.System`, preserves a final observation at
the exact deadline and throws a typed timeout containing the expected status and complete last
`BusHealthResult`. Cancellation wins before the first observation and preserves the exact caller
token. Collection waiting materializes and validates its input once, starts all waits together and
returns results in input order. No custom clock, real sleep, elapsed-time oracle or test-framework
helper is part of this owner.

The final source-derived review added two boundaries not present in the inherited tests: a state
reached only after the deadline cannot revive the wait, and a deferred collection is enumerated
exactly once. Both were proven with isolated product mutations. The complete native profiles and
all direct callers are green; the retained kill-switch fixtures are only API-adjusted because their
state-machine behavior has not yet been migrated.

## Kill-switch lifecycle and recovery

The imported state classes describe a useful protection mechanism but not a coherent modern
lifecycle. `Ready` bypasses the nominal recovery population, transition work is detached, failed
restart strands the switch, and counter reset races concurrent observations. Raw timers, stopwatch
and process UTC form three time sources. The current upstream source remains materially identical,
so it supplies no correction that should be copied.

The coherent feature is smaller than the imported API: observe failures, trip one endpoint after an
exact population/ratio decision, expose degraded health while it is paused, then restart reliably.
One internal state machine can own this with an immutable configuration snapshot, `TimeProvider`, a
stored recovery task and bounded retry. The former runtime interfaces and state classes are not
extension points and have no product consumer; their only outside consumer is the inherited
instrumentation fixture, which must be replaced by behavior against the real recovery boundary.

The old `RestartingKillSwitchState` nevertheless records useful experienced-author intent: a
matching failure after restart should stop the endpoint immediately, while enough successful
deliveries should stabilize it. `KillSwitch.Ready` replaced that state with `Started` before any
delivery could exercise it, and the nominal success boundary was one attempt late. The final design
recovers the intent as an explicit internal `VerifyingRecovery` state with an exact inclusive
activation boundary. It does not preserve the dead public state graph or compatibility API.

The final matrix has 26 one-to-one projected facts and 85 assertions. Fifteen isolated mutations,
including both recovery-verification exits, fail for their intended cause. Core is 828/828,
UnitArchitecture is 1455/1455 and LocalIntegration is 3/3, all with no failures or skips; the full
serial Engineering Release build is clean.

## Fault diagnostics and host metadata

The inherited exception tests preserve two useful transport facts but do not detect that
`FaultExceptionInfo` aliases a generic `Exception.Data` dictionary while copying a non-generic one.
That makes an already-created fault change when application code later mutates the exception and
also makes key comparison depend on the source dictionary. The corrected owner always takes one
ordinal-ignore-case snapshot. Application-wrapper values win over wrapped exception values, while
the wrapped exception remains the reported identity.

The transport test also exposed a separate owner defect in
`CaseInsensitiveDictionaryStringObjectJsonConverter`: the public `Write` override selected itself
again instead of the intended enumerable overload. Naming the implementation `WriteEntries`
removes the ambiguous overload and exercises the real System.Text.Json receive boundary rather than
merely observing a pre-serialization publish context.

`BusHostInfo(bool)` did not represent two modes; its argument was ignored. The serializer still
needs the public parameterless DTO constructor, while production capture now has one internal
factory using `Environment.ProcessId` and `Environment.ProcessPath`. The cache remains the lifetime
owner. The new transport test waits for a dynamically connected endpoint to become ready before
publishing, so all eight fields are proven after real envelope serialization.

Seven isolated mutations reject aliasing, comparer drift, omitted JSON entries, wrapper identity,
placeholder process identity, a revived boolean constructor and requirement-projection drift. The
complete final profile is 1466/1466, LocalIntegration is 3/3, and all Release builds are clean.

## Initializer duplicate-path consolidation

`SendProxy_Specs.cs` is named as though it tested a send proxy, but it only invokes
`MessageInitializerCache<T>.Initialize` and compares Guid, string and `DateTime` properties. The
accepted exact-scalar test covers string and `DateTime` plus the complete scalar set; the accepted
dynamic-contract test covers Guid/string anonymous materialization and the real interface transport
boundary. Their union strictly contains the old assertion set, so another test would add a second
truth rather than coverage.

## InMemory receive-endpoint concurrency

The inherited `Threading_Specs.cs` title describes thread-pool scaling, but its actual contract is
transport-level concurrency: `InMemoryReceiveTransport` creates a `TaskExecutor` from
`ConcurrentMessageLimit ?? PrefetchCount`, and the single fabric dispatcher can continue because
`TaskExecutor.Push` completes after admission rather than after handler completion. The relevant
owner path is endpoint configuration -> receive context -> InMemory receive transport -> executor;
the existing executor and middleware tests do not prove that the endpoint configuration reaches
that path.

The native replacement therefore sends real messages to the harness input endpoint. It first
saturates one hundred configured slots while holding every handler, preserving the inherited high
concurrency boundary. A separate cap case configures prefetch four and concurrency three, proving
that the fourth handler cannot enter until a slot is released. This also gives a precise mutation:
using prefetch unconditionally produces an observed maximum of four and fails the cap assertion.

## Structured bus-probe endpoint inventory

`Introspection_Specs.cs` only asserts endpoint addresses, despite configuring unrelated handler,
rate-limit and consumer behavior. Its `GetReceiveEndpointAddresses` call is not a core API: the old
TestFramework helper serializes `ProbeResult` with System.Text.Json and reparses a fixed JSON path.
The product already exposes the richer structured `ProbeResult`, whose relevant path is bus -> host
-> receiveEndpoint -> receiveTransport -> address.

The harness owns an input endpoint and an internal bus endpoint; a connected dynamic endpoint is a
third. The native replacement asserts all three unique addresses directly in the structured graph.
After stopping the dynamic handle, its collection entry is removed synchronously and the next probe
contains exactly the two persistent endpoints. Renaming the transport's `address` field makes both
tests fail at that exact missing key, proving that neither test merely counts unrelated nodes.

## InMemory outbox fault isolation

The inherited `Outbox_Specs.cs` proves one useful contract but uses a 300-ms negative timeout and
does not await the response that is supposed to be deferred. The actual owner path is
`InMemoryOutboxSpecification` -> `InMemoryOutboxFilter` -> `InMemoryOutboxConsumeContext` -> the
deferred outbox send endpoint. The filter executes queued actions only after the handler succeeds;
on a handler exception it must discard them before the consume pipeline publishes `Fault<T>`.

The native replacement awaits `RespondAsync`, then throws. The observed request fault is a causal
barrier: by then the outbox catch path has completed and the fault has been published. A canceled
snapshot token inspects already observed sent messages without introducing another wait. This
proves both the exact fault envelope and that no deferred response escaped.

The isolated catch-path mutation from discard to execute made the response assertion fail. A first
attempt to reverse that mutation matched the identical call in the success branch and left the
branches inverted; the still-red focused test correctly rejected it. The complete branch was then
restored explicitly, the product file was proven byte-identical to its baseline, and a forced
non-incremental build preceded the final green run. This is now the required restoration procedure
for mutations with repeated or structurally similar call sites.

## Preliminary next-owner triage: dynamic consume-pipe connections

Do not migrate `ConnectHandler_Specs.cs` as an isolated one-test cohort. Its apparent happy path is
one member of a shared dynamic connection boundary containing `ConnectHandler`, `ConnectConsumer`,
`ConnectInstance`, consume-message observers and their `ConnectHandle` lifetimes. The adjacent R0
set is `OBL-R0-CORE-B-0430` through `OBL-R0-CORE-B-0437`; treating only 0433 would preserve the old
suite's missing disconnect, lifecycle and error-order coverage.

A preliminary read shows that the eventual source-owner cohort must disposition handler, factory,
object-instance and multi-message consumer attachment together; prove observer pre/post/fault order
and exact fault identity; and derive connection/disconnection plus concurrent update boundaries from
the complete connector, pipe and handle implementation before writing tests. This is a triage note,
not an implementation plan: the full source graph has not yet been read and no fixture is replaced.

The direct connector and dispatch path has now been read far enough to sharpen the next analysis:

- one consumer implementing two message interfaces creates two message connectors and returns one
  `MultipleConnectHandle`; disconnect must remove both routes, and partial connection failure must
  roll back every already-created route;
- `Connectable<T>` already has strong native lifecycle, snapshot, null-task and failure-aggregation
  coverage, so the next cohort should reuse that truth rather than duplicate its unit tests;
- existing native `ConsumeObserverTests` are stronger than most inherited observer assertions at a
  real bus/mediator boundary, but they do not by themselves prove the consumer-specific dynamic
  connection or the exact direct-pipe callback ordering;
- existing `InstanceExtensionsTests` prove configured endpoint registration, not dynamic
  `ConnectInstance`, while the mediator forwarding test exercises `ConnectHandler` through a
  different connector boundary;
- public handler connection currently lacks the explicit boundary validation already present on
  consumer and instance connection extensions, and several connector/cache implementation types are
  public. Their A+ API disposition must follow the complete call-site and package-surface review,
  not be preserved merely for compatibility.

The tee/request routing and handle composition have now also been read. Dynamic message routes end
in a `RequestIdTeeFilter<T>` whose ordinary route is a `Connectable<IPipe<T>>`; request-key routes
add one lazy key filter and return the keyed connection handle. Dispatch observes one stable
connection snapshot, awaits all started branches, and only then invokes the continuation. Consumer
and instance connectors dispose already-created handles if a later message-interface connection
throws. The preliminary ordinary matrix can therefore be bounded to:

1. handler delivery, validation and disconnect;
2. factory and exact-instance delivery plus disconnect;
3. one consumer implementing two message interfaces, one composite handle and removal of both
   routes;
4. injected second-route failure with rollback of the first route;
5. typed observer order `pre -> handler/consumer -> post`, exact fault rethrow, no post on fault and
   observer disconnect;
6. a source/API disposition that keeps useful public extension points while internalizing accidental
   connector/cache implementation surface where package and call-site evidence permits it.

The consumer metadata and repository-wide call-site read corrects a tempting over-cleanup. Cache
types, handler-connector types and their cache interfaces are referenced only inside the product and
are candidates for internalization. The message-connector contracts are different:
`IMessageInterfaceType` is returned by the public custom consumer-convention mechanism and creates
consumer and instance message connectors. Removing that surface wholesale would remove a useful
extension feature, not merely a legacy API.

The eventual A+ surface review must therefore separate accidental cache/factory exposure from the
custom-convention extension boundary. It may simplify or replace the latter with a cleaner public
contract because backward API compatibility is not required, but it must retain the capability and
prove a custom convention end to end. The repository contains no `PublicAPI.Shipped.txt` or
`PublicAPI.Unshipped.txt`; package/API comparison must therefore use compiled public metadata. Then
reuse strict containment from existing native observer and `Connectable<T>` cases rather than
cloning already-proven behavior.

The complete custom-convention call-site read adds an important lifecycle constraint. The useful
feature is real: the inherited fixture registers an `IHandler<T>` convention, supplies its own
message-interface descriptor, connector factory and consumer filter, and dispatches two unrelated
message contracts through one handler type. No retained product, native test, tool, benchmark or
sample registers another custom convention, so this fixture is the only executable evidence for the
extension point.

The present API is nevertheless not an A+ lifetime owner. `ConsumerConventionCache` stores a
process-global mutable `List<IConsumerConvention>` without synchronization, while
`ConsumerMetadataCache<T>` snapshots that list once in a per-type static `Lazy`. Registering or
removing a convention after a consumer type has first been inspected therefore cannot update that
type's metadata, and concurrent registration/enumeration has no defined safety boundary. The old
fixture avoids the problem only by registering before the first use of its unique handler type and
removing afterward; it does not prove a sound runtime lifecycle. Git history available in this fork
contains only the upstream import and mechanical ViciOne rename for these files, so it supplies no
experienced-author rationale for preserving the global cache shape.

The next design must preserve custom message-discovery and connector creation while replacing the
global mutable registration/cache coupling with one explicit, immutable convention set whose
lifetime is owned by bus configuration. Built-in async, batch and job conventions belong in that
default set. The exact public contract still requires the full compiled-surface and construction
path review; no API change is authorized by this preliminary note.

The public dynamic-connection facade also has inconsistent guard ownership. `ConnectConsumer` and
`ConnectInstance` validate their connector and subject inputs before touching caches, whereas
`Handler`, `ConnectHandler` and `ConnectRequestHandler` forward unchecked inputs. The ordinary
handler connector creates a default pipe configurator when none is supplied, but the request
variant dereferences its required configurator immediately. These are source-observed boundaries,
not yet accepted defects: the next cohort must read every request-handler caller and specification
owner before deciding whether to normalize the public API or retain a deliberate distinction.

Do not accidentally merge the separate `IObserver<ConsumeContext<T>>` endpoint/bus connector API
into this cohort. R0 obligations 0434--0437 exercise `IConsumeMessageObserver<T>` on
`ConsumeContextOutputMessageTypeFilter<T>`: pre callbacks precede dynamic dispatch, post callbacks
follow it, fault callbacks observe its exact exception, and the exception is rethrown. The generic
observer facade builds a different pipe through `ObserverConnectorCache<T>` and has its own public
guard and lifecycle questions. It is adjacent implementation, not evidence for this obligation
set, and requires a separate source-owner disposition if selected later.

The configuration path confirms another custom-convention mismatch. `UntypedConsumerConfigurator`
correctly obtains message specifications through the registered conventions, so the inherited
`IHandler<T>` convention executes. Its validator nevertheless warns solely because the type does
not implement the built-in `IConsumer` marker, even when the convention has discovered valid
message connectors. A greenfield convention contract must make discovery and validation consume
the same immutable result; a valid custom consumer must not produce a false built-in-marker
warning. This is a candidate product defect to reproduce before fixing, not permission to weaken
consumer validation globally.

Convention order is behavior, not incidental list order. The current defaults are async, batch and
job; metadata groups descriptors by message type and keeps the last descriptor, and subsequently
registered custom conventions are appended. This lets a specialized or custom connector override a
generic descriptor for the same message type. Any immutable replacement must make this precedence
explicit and deterministic, including duplicate registration and conflicting custom conventions,
rather than sorting or deduplicating the set accidentally.

`Connectable<T>` itself already has strong native snapshot, idempotence, concurrent-disconnect and
multi-failure tests. Its composite owner does not: `MultipleConnectHandle` retains a caller-supplied
array by reference, validates neither the collection nor its elements, and stops disconnecting as
soon as one child throws. The consumer/instance rollback loops have the same stop-at-first-failure
shape. The next cohort must determine the intended cleanup contract from all handle owners and
history, then prove that one faulty child cannot leave unrelated dynamic routes connected. Do not
rewrite `Connectable<T>` or duplicate its already accepted tests to hide this separate composite
lifetime question.

The Microsoft Roslyn static-pairing analyzer reports `ConsumerConnectorCache.cs` as unpaired, which
supports selecting this cluster for deeper analysis. Its wider count (3911 source files, 3140
statically unpaired) is only a parse heuristic: extension-method calls, DI and reflection cause
false negatives. Its suggested paths into the inherited `ViciOne.ServiceBus.TestFramework` are
invalid for the accepted architecture because the analyzer derives them from still-existing project
references. Use the report for triage only; source-derived behavior, R0 obligations and executable
native tests remain the acceptance truth.

## Native test source-layout enforcement

The compiled architecture rules protected assembly and dependency direction but did not bind C#
source namespaces to physical folders. With 280 native source files, that left the structure rule as
prose and allowed silent navigation drift.

The new rule consumes evaluated MSBuild `Compile` items and `RootNamespace` values, then uses Roslyn
syntax rather than text matching. It exposed nine real discrepancies in two support projects. Their
files already shared the intended public architecture roots
`ViciOne.ServiceBus.Tests.Infrastructure.Analyzers` and `.Roslyn`; only MSBuild was deriving a
different implicit root from the assembly name. Explicit project-level roots make both truths clear:
the assembly name identifies the specialized artifact, and the namespace identifies its common
test-infrastructure owner. No namespace or public surface was renamed.

An isolated file under `Serialization` with namespace `ViciOne.ServiceBus.Tests.WrongFolder` made
exactly the new rule fail while the other 84 architecture facts remained green. After removing the
probe, the complete architecture project returned to 85/85. A second isolated file declared the
correct namespace first and a hidden second namespace afterward; it also made exactly the new rule
fail, this time with the exact two-declaration diagnostic. Its removal again restored 85/85.
