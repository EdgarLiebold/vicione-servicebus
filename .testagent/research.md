# Research — C1a Abstractions MessageBody contract

Scope: first hermetic behavioral cohort after the accepted F1a/F1b native xUnit 4 / Microsoft
Testing Platform 2 foundation. Product baseline is commit `2c38bbbbd6fc2ade1b7b42b6adf72a9bda53b03a`,
tree `456000b16ba00a003dcb293074175027103f598d`.

## Sources read completely

- `MessageBody` and all five concrete implementations in `src/ViciOne.ServiceBus.Abstractions`;
- inherited `tests/ViciOne.ServiceBus.Tests/Serialization/MessageBodyLength_Specs.cs`;
- all 87 final R0 obligations for that inherited file;
- the accepted native-test implementation contract and F1b projection implementation;
- mandatory Microsoft .NET testing and MSBuild skills.

## Selected closure

C1a replaces exactly 51 Abstractions obligations. The other 36 obligations, including the global
cross-assembly census, remain open and prevent deletion of the inherited file.

The 51 rows consolidate into 13 replacement Facts. For each of eight inherited input variants,
fresh subjects exercise Length, bytes, text, and stream as the first accessor plus a stability
comparison. Exact external bytes and text are the oracles. Stream tests perform a real write,
require `NotSupportedException`, and verify unchanged body bytes.

Two additional Facts harden empty and invalid Base64. Two Assurance Facts verify the exact five-type
set and the passive requirement projection. These four do not claim an inherited obligation.

## Fixed architecture

- source-owner project `tests2/Core/ViciOne.ServiceBus.Abstractions.Tests`;
- 17 parameterless xUnit Facts, no Theory and no Skip;
- executable MTP project with exactly one direct `xunit.v3.mtp-v2` entry;
- framework-neutral shared projection verifier returning diagnostics, never a test verdict;
- one ordinary xUnit assertion owns each projection verdict;
- no product edit, old-test edit, second runner, Python test platform, receipt, sentinel, time,
  randomness, file, environment, network, or mutable global state.

The detailed Lead plan and both immutable projections are authoritative for exact names and mapping.
Two independent pre-start reviews passed after correction with no open blocker or major finding.
