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
