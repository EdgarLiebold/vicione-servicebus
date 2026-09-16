# Iteration 167 — EF ambient-context lifecycle admission

## Result

This packet personally reads both complete EF ambient consume-context implementations (103 lines)
and both complete owning test files (708 lines). The review found one shared lifecycle defect with
two manifestations. The reliable implementation validated `ConsumeContext` only after its base
constructor had completed, so a rejected construction could leave the base DbContext event
subscription behind. The transactional implementation did not reject a null consume context at
all and could construct endpoint/client wrappers around invalid ambient state.

Both constructors now validate `ConsumeContext` while evaluating the base-constructor arguments,
before any base-constructor service resolution or event subscription can occur. The reliable type
also removes three redundant derived null checks already owned by the base constructor. The two
implementations consequently share one explicit boundary and one dependency-ownership model.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 103 | `b5df7e011debfdee1ebef131cc121861a8fc6c9c188c0286af247a1a7e27a026` | `e5e79ccd6ad2ab6496b3b74664f0eedb3e88398c688127f62f8e546592fad365` |
| Tests | 2 / 708 | `4bc1d5fbf376bc94625b156a7daf16f8b9c59941b05afefe07e9d6ad0dbed79c` | `4108b2f0cbd7ca0770cceb99b546e2cdefa2dac0e609754ee3efad3edd860abb` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 166 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkConsumeContextScopedBusContext.cs` | `316b3f9856fe1062313467adfa6e5bd6dd622d147798f33075f0e6c114a3dfa9` |
| Source | `EntityFrameworkTransactionalConsumeContextScopedBusContext.cs` | `44c9655d21a4279d028e7120bfdab337b848910c06bcb61543307ac35a789ba1` |
| Test | `EntityFrameworkReliableMessagingRegistrationTests.cs` | `ec11dff5eeae15f3d43339037fd646e8c36b6fd1b186fe7d662f82049fc637a4` |
| Test | `EntityFrameworkTransactionalScopedBusContextTests.cs` | `7f76ff611be23cead9c3f95b52a216fd83c8b6a0360e6598084b99777e27cb43` |

Cumulative personal source admission is 296/4,116 current C# files.

## Proof

The combined owning run contains eighteen discovered cases. The two ambient cases pass every other
constructor argument as null and still require `consumeContext` to fail first, which proves that
the new guard executes before the base constructor. Valid contexts additionally prove non-null,
stable lazy send, publish and request-client views for both implementations.

Six successfully compiled single-cause mutants were killed and restored: remove the reliable
pre-base guard, remove the transactional pre-base guard, return null from each reliable send,
publish and client projection, and return null from the transactional client projection. Both
product sources were restored to the reviewed final bytes before the final gates.

Baseline reliable-context coverage was 66.67% line and 50% branch at complexity 8; the transactional
context was 100% line/branch but had no null-boundary contract. Final combined Cobertura is
`/private/tmp/vicione-servicebus-iteration-167-final.cobertura.xml`, SHA-256
`a37cdd32e94021dfba3c536d8f05ea57e938d63d45e86499f9efdba3e68d4fcc`.
Both classes now report 100% line and branch coverage; maximum complexity and CRAP are both 1 for
each class. Unit sorted-display-name SHA-256 is
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

| Gate | Result |
| --- | --- |
| Combined ambient-context owning tests | 18/18 passed |
| Full EF unit | 248/248 passed |
| Strict Release product/EF/unit/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-167-ef-ambient-context-lifecycle-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. Remote publication remains an independent delivery step and cannot pause or deactivate
the active goal.
