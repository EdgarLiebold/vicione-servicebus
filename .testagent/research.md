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
