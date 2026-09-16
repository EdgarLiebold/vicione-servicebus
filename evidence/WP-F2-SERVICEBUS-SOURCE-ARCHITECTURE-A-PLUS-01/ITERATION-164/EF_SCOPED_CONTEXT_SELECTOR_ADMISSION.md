# Iteration 164 — EF scoped context selector admission

## Result

This packet personally reads the complete EF scoped bus context selector source (55 lines) and its
complete owning test file (299 lines). The review found two fail-fast gaps at the DI boundary: a
null factory registration produced an incidental `NullReferenceException`, while a selected factory
could return null and silently publish an invalid `Context` property.

The retained correction validates every materialized factory registration before selection and
rejects a null factory result with the same structured `ConfigurationException` used by the other
selector invariants. No factory is invoked when the registration set itself is malformed. The
existing deterministic policy remains unchanged: one registration is the implicit default;
multiple registrations require exactly one explicit default.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 55 | `afce95477bfcf0f95fedd9b7179a2c52a05045cd8b2c84c611ed2b44d259f954` | `555be1336f5abc0bbc9b142f374b01a4f3e4b20a18187bd0bd684b505aafe1ed` |
| Tests | 1 / 299 | `2d7830f3f791dc33d9fa3978a37bdfce9e935f5d818f7471e836ad71145e6c20` | `6df6c16d9688bc53923b824c4f56990cecc7e42a4c74d89ba5bdf5eef924eb63` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 163 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkScopedBusContextProvider.cs` | `c4bfbfb820327eb316d244e026eaed59c1e64e0883b7a8fc95979972da4328d5` |
| Test | `EntityFrameworkScopedBusContextProviderTests.cs` | `f4e6975612f1631263c4ebdbb2b90ac0cd13cc0fa43c6455bb70775ec2f7a313` |

Cumulative personal source admission is 291/4,116 current C# files.

## Proof

The owning suite now contains twelve discovered cases. The new case proves an exact argument
failure for a null registration, zero factory invocations for that malformed set, a structured
configuration failure for a null selected result and exactly one attempted factory invocation.
The requirement manifest binds the case to
`selector-rejects-null-registration-and-context-result`.

Six compiled single-cause mutants were killed and restored: remove null-registration validation,
accept a null factory result, remove implicit single-registration selection, make explicit-default
selection order-dependent, accept multiple registrations without a default and accept multiple
defaults. A final gate detected an intermediate restoration mismatch in the single/empty branches;
the intended code was restored, rebuilt and every final gate below was rerun successfully.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-164-final.cobertura.xml`,
SHA-256 `a802bf9a7b9442217727555b6c8d69ef4e15fdf17b088d25f3e03b0bf433aad6`.
The selector and its compiler-generated predicate report 100% line and branch coverage. Maximum
reported complexity and CRAP are both 16; no method exceeds CRAP 30. Unit sorted-display-name
SHA-256 is `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

| Gate | Result |
| --- | --- |
| Focused scoped context selector | 12/12 passed |
| Full EF unit | 247/247 passed |
| Strict Release product/EF/unit/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-164-ef-scoped-context-selector-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
