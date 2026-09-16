# Iteration 153 — EF relational-infrastructure admission

## Scope and result

This bounded packet personally rereads every current C# file/comment in the Entity Framework Core
execution-strategy, transaction-context, identifier-validation and lock-statement owner: 13 product
files / 806 lines. Three owning unit/local test files / 1,141 lines were also read in full.

Three product defects are corrected without public-API expansion:

- property-sequence cache identities are length-prefixed instead of separator-joined, preventing a
  two-property lookup from aliasing a legal separator-bearing shadow property;
- explicit fallback schemas and EF model schema/table/column names cross the existing portable
  relational-identifier validator before formatting and caching;
- provider discovery on an unconfigured `DbContext` now emits the stable actionable
  `ConfigurationException` and retains EF Core's original failure as its inner exception.

Eight focused test methods add twelve compiled cases. They also prove all retained runtime provider
selections and statement families, required inputs/mappings, provider-specific fallback-schema
semantics and a real SQLite outbox selection that cannot cross the requested bus boundary.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 13 / 806 | `76b1c5e90fa6b0993e633136cba9192fc87c8654d324361d699bbdd89716cc72` | `8f7aea3c5d93ad7713a37b5ec6aadb2ca5778dc8997dd5c2c7671e59092e3c60` |
| Owning tests | 3 / 1,141 | `b3a3c224868d44c12625b85528a78c12a0774ce1f3b128bb1cf0c7c9908bef3e` | `32a95d6bce4f11aec7578266cd86cf60d94db01600bb7648c0248ef00be4af74` |

The chains extend Iteration-152 hashes
`af10489b73f7254d9fa088fbf380f2f88e3d6a6c69dafdaadf9b1e35ad619cd2` and
`270d92585b61f2f2edb553bbef16d7ffbc4da063dec9c10b889aea953580bbdd`.
Cumulative current personal source admission is 253 of 4,116 current C# files.

## Causal test and assertion evidence

The initial unit owner passes 167/167. The final owner passes 179/179 after adding twelve cases.
The eight new methods have no assertion-free, trivial-only or self-referential case. Their checks
cover exact SQL and identity values, negative selection boundaries, exception types, parameter
names, diagnostic text, retained inner exception and collection cardinality.

Every productive correction is mutation-proved. Six hand-authored single-cause mutants were built
and run separately, and each was killed by its designated owner test:

| Mutant | Killing observation |
| --- | --- |
| Restore separator-joined cache key | separator-bearing property resolves the cached two-property mapping |
| Bypass fallback-schema validator | unsafe fallback schema constructs successfully |
| Rethrow raw EF provider failure | expected `ConfigurationException`, observed `InvalidOperationException` |
| Bypass mapped-schema validator | unsafe model schema produces SQL |
| Bypass mapped-table validator | unsafe model table produces SQL |
| Bypass mapped-column validator | oversized UTF-8 model column produces SQL |

The restored source passes all final gates. No mutant remains in the working tree.

## Pairing, coverage and risk

The isolated analyzer input `/private/tmp/vsb-iteration153-pairing.HE0Zpo` contains exactly the 13
admitted source files and the bounded EF unit/local test corpus. Roslyn static pairing reports 10/13
direct pairs. `RelationalIdentifierValidator.cs`, `PostgreSqlLockStatementFormatter.cs` and
`SqlServerLockStatementFormatter.cs` are internal helpers exercised transitively through provider
tests and runtime instrumentation. Static pairing is a name/reference heuristic and is not treated
as runtime proof.

Fresh terminal instrumentation covers 249/261 lines (95.4023%) and 56/68 branches (82.3529%) across
61 methods. No method exceeds CRAP 30; maximum CRAP is 14.4970 in `ResolveMapping`. The final sorted
unit-test-name SHA-256 is
`d5fc4b039c2c72386a38763a36d488bf64f72145d745721d52e3141cea4d879a`.
Cobertura SHA-256 is
`44c4278c08c57a71d369119ba62cc80f554ccfaf77a60ef4537a48863121a2f8`.
Final project, lock and requirement-projection hashes are
`6b1545738ab188c33163e5cd7ffdfee2fdc888d5032a4558509b17b802410720`,
`f0a75551e91331583dea2d3e541366645678ac696bfe6235aa59cb8355df8aef` and
`69d81fa428f5bcf78ce548bcb57d392cf204e40b108c2ace0f97b80ab6a2b46b`.

Uncovered lines are defensive post-`ExceptionDispatchInfo.Throw` rethrows/no-op guards, missing EF
table/column metadata guards and the unknown third-party provider adapter diagnostic. They were
manually classified; measured thresholds remain comfortably met without synthetic provider state.

## Gates and provider boundary

| Gate | Result | Duration |
| --- | --- | ---: |
| Unit baseline | 167 passed; 0 failed/skipped/other | — |
| Unit final coverage | 179 passed; 0 failed/skipped/other | 34.870 s |
| Local requirement projection | 1 passed | 0.521 s |
| Local PostgreSQL attempts | 59 blocked by missing profile before product behavior; projection passed | 0.696 s |
| Strict product Release build | 0 warnings / 0 errors | 19.45 s |
| Strict unit-owner Release build | 0 warnings / 0 errors | 74.29 s |
| Strict local-owner Release build | 0 warnings / 0 errors | 57.43 s |
| Product/unit/local format verification | Exit 0, no differences | — |
| Native Core gate | 4,799 passed; 0 failed/skipped/other | 31.796 s |

Core sorted-name SHA-256 remains
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.
The local provider lane was attempted as configured; no endpoint or credential values were invented
or committed, so PostgreSQL acceptance remains explicitly open.

## Checkpoint

Raw artifacts remain under `/private/tmp/vsb-iteration153-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence. Intended tag:
`servicebus-a-plus-iteration-153-ef-relational-infrastructure-admission-2026-09-16`.

Whole-fork personal source/comment completion, global API/naming/coverage gates and configured
external-provider acceptance remain open. This packet proves only the bounded current EF relational
infrastructure owner and its fresh local evidence.
