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

That Abstractions cohort changed no production source or inherited test.

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
- delete only `MessageUrnSpecs.cs`; retain `MessageType_Specs.cs` until its independent array
  publish/consume obligation is replaced;
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
