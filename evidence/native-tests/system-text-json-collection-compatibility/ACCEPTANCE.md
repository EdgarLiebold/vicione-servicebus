# System.Text.Json Collection Compatibility Acceptance

Date: 2026-08-23

## Scope

- The inherited `MoreSerialization_Specs.cs` owned 34 ledger identities: 17 MessagePack identities
  already closed by the accepted MessagePack cohort and 17 System.Text.Json Core identities.
- Nine ordinary xUnit methods add 13 executing cases for the open collection and contract shapes.
  Two existing, stronger facts remain the owners for constructor-bound private-setter values and
  ordered key/value lists with duplicate keys.
- Test contracts and tests reside under the real `ViciOne.ServiceBus/Serialization` source owner.
  No product source, public API, serializer, package, test framework, clock, network, or external
  service changed.
- The 34/34 disposition binds the exact inherited ledger, deleted fixture, and accepted MessagePack
  disposition by SHA-256.

## Executed acceptance

- Focused collection class: 17 total, 17 passed, 0 failed, 0 skipped.
- Native Core project: 382 total, 382 passed, 0 failed, 0 skipped.
- UnitArchitecture solution: 899 total, 899 passed, 0 failed, 0 skipped.
- LocalIntegration solution: 3 total, 3 passed, 0 failed, 0 skipped.
- Non-incremental Release builds: 0 warnings, 0 errors for both profiles.
- Remaining inherited Core test project after deletion: 0 warnings, 0 errors in the bounded
  compile-only proof; product references and analyzers were already covered by the native complete
  build.
- Bounded `dotnet format --verify-no-changes`: passed for the changed C# file. The repository-wide
  formatter still reports pre-existing inherited product formatting and is not represented as a
  cohort regression.
- Five one-cause mutations: all rejected for their intended behavior or projection reason.
- Recursive inherited-tree check: no empty directory remains under `tests/`.

## Static quality review

Every changed line and every final test method was read after restoration of the mutants. Subjects
and expected collections do not share mutable collection instances. Assertions preserve exact
cardinality, keys, values, membership, ordering, type, and non-aliasing as applicable. No assertion
depends on inherited equality beyond immutable value records, elapsed time, randomness, polling,
skip paths, files, environment state, or external infrastructure.

Verdict: **PASS**.
