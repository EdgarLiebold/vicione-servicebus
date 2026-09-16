# Iteration 166 — EF scoped-context factories admission

## Result

This packet personally reads both complete EF scoped-context factory sources (136 lines) and the
complete reliable-registration owning test file (276 lines). The product review found both factory
implementations correct: each rejects a null provider, preserves a matching ambient bus consume
context, resolves the configured scoped context otherwise, and constructs the correct explicit
transactional context for ambient and non-ambient execution.

No product defect was retained. The admission closes the remaining proof gap in the reliable
factory's static transactional-context path: the existing ambient consume-context case now proves
that this path constructs `EntityFrameworkConsumeContextScopedBusContext`, rather than only proving
the public selector path.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 136 | `51aa1a3edb2793af57a0158aaf960469255a63af66788143101511a5637a2eeb` | `3e39877d0409ff91b3a1519fc9d26357cfcfb35bb4691a6f9686a52ab53a8b5f` |
| Tests | 1 / 276 | `566970d721b83227cdbf203d9b084d587248e7a218182bd40b861eba8a4ee471` | `f2a53f625db6610710a4ffab243570d2826adceb5b3223c42777a345cf817fb8` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 165 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkScopedBusContextFactory.cs` | `7a581cfdd6da9c511c4aa3946407cded9d61b16e0ef2180d5f166208356bd4a3` |
| Source | `EntityFrameworkTransactionalScopedBusContextFactory.cs` | `03956c07dae027cc168ed764cd4073c509c5f31ae47c8021c7dac1882d3764af` |
| Test | `EntityFrameworkReliableMessagingRegistrationTests.cs` | `56dbfc750a9b641dcb7c02dcf4bdcbc54ab545a4a9d15b44a8b6c8e6802c0b8e` |

Cumulative personal source admission is 294/4,116 current C# files.

## Proof

The combined owning run contains eighteen discovered cases: six reliable-registration cases and
twelve selector/transactional cases. It proves both factories' selector and explicit-transactional
ambient/non-ambient branches, default-selection metadata, null-provider guards, registration
identity and deterministic selector behavior. The strengthened case remains bound to the existing
`reliable-factory-preserves-ambient-consume-context` requirement variant.

Six successfully compiled single-cause mutants were killed and restored: invert each factory's
public ambient-context guard, invert each factory's static transactional ambient-context guard, and
invert each factory's `IsDefault` value. Both product sources were restored byte-for-byte before
final gates.

Final combined Cobertura is `/private/tmp/vicione-servicebus-iteration-166-final.cobertura.xml`,
SHA-256 `36b90ae7094eaa7a70139b3119f4fa9880e9ae5e87402913e3e2b509e9114f48`.
Both factory classes report 100% line and branch coverage; maximum complexity and CRAP are both 4
for each class. Unit sorted-display-name SHA-256 is
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

| Gate | Result |
| --- | --- |
| Combined factory owning tests | 18/18 passed |
| Reliable-registration focused tests | 6/6 passed |
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
`servicebus-a-plus-iteration-166-ef-scoped-context-factories-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open. A remote publication problem cannot pause or deactivate the goal; local admission work
continues independently.
