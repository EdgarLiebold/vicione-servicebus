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
