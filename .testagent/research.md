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
