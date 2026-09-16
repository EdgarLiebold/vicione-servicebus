# Iteration 152 — DynamoDB read admission

## Scope and result

This bounded packet personally rereads all current C# files/comments in
`ViciOne.ServiceBus.DynamoDb`, both owning test projects, their fixture and requirement projections:
13 product files / 1,061 lines and nine final test/support files / 1,933 lines.

No current correctness, lifetime, cancellation, serialization, concurrency, dependency, comment,
placement or public-API defect was reproduced. Product code is unchanged. Seven isolated owner tests
now prove public repository execution, repository/load-context forwarding and cancellation, provider
ownership, context-factory execution/send/probe/query boundaries, and typed concurrency identity.
The centrally pinned MTP coverage package and deterministic lock entries enable fresh owner coverage.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 13 / 1,061 | `4159cdba8a4c86c22bfc50c5a8de17d14af5ca9767924c41961ec753a6b0c279` | `af10489b73f7254d9fa088fbf380f2f88e3d6a6c69dafdaadf9b1e35ad619cd2` |
| Owning test/support | 9 / 1,933 | `5901c497e0405dc9154400680c3c89875e8902af6b994aa635358dd08e18595e` | `270d92585b61f2f2edb553bbef16d7ffbc4da063dec9c10b889aea953580bbdd` |

The chains extend Iteration-151 hashes
`1ec77d5396ff7e18028267f8a8d8d9271d479f03dcd8d059b4195e98a601b13e` and
`9e67955dec032c0e49e6e3baddd8da304a8330c275856b86b6f249dadcfd8509`.
Cumulative current personal source admission is 240 of 4,116 current C# files.

## Review and focused hardening

Personal review covers the minimal API, immutable configuration, DynamoDB table names, TTL and clock
injection, per-saga factory isolation, operation-scoped context ownership, serialized identity and
version validation, conditional create/update/delete, overflow and failed-update restoration,
cancellation identity, and provider-backed persistence/concurrency scenarios.

The initial 21-test owner already fully covered store/configuration core behavior but left public
repository and context orchestration dependent on transitive or LocalStack execution. The seven new
tests directly pin these boundaries. Mutation-oriented review confirms that they fail for dropped
forwarding, token substitution, removed guards, missing disposal, bypassed repository execution,
lost exception identity/cause or accepted query correlation. No shallow or assertion-free addition
was admitted.

## Pairing, coverage and risk

The isolated analyzer input `/private/tmp/vsb-iteration152-pairing` contains exactly 13 source and
nine test files. Roslyn static pairing reports 13/13 paired and zero unpaired. This is a static
name/reference heuristic and is reported alongside, not instead of, runtime coverage.

Fresh final instrumentation covers 350/388 lines (90.2062%) and 50/57 branches (87.7193%) across
60 methods, package complexity 127. No method exceeds CRAP 30; the maximum is 30. The final sorted
unit-test-name SHA-256 is
`ced3540634c13a1399f5c3319ccd8af0b22b89b001f4da67e4276c128950bf62`.
Cobertura SHA-256 is
`27b3cf183696f911e2624bc39cfed5c47b2917d82458f60b303aa3559eb456a2`.
Final project/lock hashes are `18f15b44077e404762ae11e30954899641a7f4fd9c0ca8f4438273cc9bbbc354`
and `7bb6ed310ae7c96e12a32e9c41527a37dfd6b48d33943ab09ae731f52ab30385`.

## Provider boundary and validation

The local requirement projection passes. All eight real DynamoDB/LocalStack cases stop in the
canonical fixture before product behavior because profile `UnitArchitecture` has no configured
LocalStack endpoint or credentials. Values were not fabricated or committed; provider acceptance
remains explicitly open.

| Gate | Result | Duration |
| --- | --- | ---: |
| Unit baseline | 21 passed; 0 failed/skipped/other | 1.651 s |
| Unit final coverage | 28 passed; 0 failed/skipped/other | 10.413 s |
| Local requirement projection | 1 passed | — |
| Local provider attempts | 8 blocked by missing profile before product behavior | — |
| Strict product Release build | 0 warnings / 0 errors | 47.68 s |
| Strict unit-owner Release build | 0 warnings / 0 errors | 39.51 s |
| Strict local-owner Release build | 0 warnings / 0 errors | 61.33 s |
| Product/unit/local format verification | Exit 0, no differences | — |
| Native Core gate | 4,799 passed; 0 failed/skipped/other | 22.455 s |

Core sorted-name SHA-256 remains
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

## Checkpoint

Raw artifacts remain under `/private/tmp/vsb-iteration152-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence. Intended tag:
`servicebus-a-plus-iteration-152-dynamodb-read-admission-2026-09-16`.

Whole-fork admission, global API/naming/coverage checks and configured real-provider acceptance
remain open. This packet proves only the current DynamoDB owner and its fresh local evidence.
