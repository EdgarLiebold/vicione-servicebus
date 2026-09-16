# Iteration 150 — Azure Storage read admission

## Scope and result

This bounded packet personally rereads all current C# files and comments in
`ViciOne.ServiceBus.Azure.Storage`, its project metadata, both directly owning test projects,
their support fixture and bound requirement projections. The owner has 6 C# files / 575 lines; its
two test owners contain 7 relevant C# files / 1,065 lines.

No current correctness, lifetime, cancellation, naming, namespace, placement, dependency,
comment or public-API defect was reproduced. The admission changes no product or C# test code. It
adds the centrally pinned repository-standard `Microsoft.Testing.Extensions.CodeCoverage` package
to the unit owner and records only that package's deterministic direct/transitive lock entries.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 6 / 575 | `87222551cd9447835f0ee1f650041c34e966342f2d6f25a268079cccd7a3dc21` | `6df76f6b74131c694e90e31751d5b081c33dc3b7815a88e7a7b87b9dcbcf2165` |
| Owning test/support | 7 / 1,065 | `3275153ea730174dcd68d00981c1a0e214c34ea8dcf75ab832e74517d8680e10` | `f2fdad3eef2b47a8874c6c369371c890196e0be9a89ebfc8d6356631bfd01f37` |

The chains extend Iteration-149 chain hashes
`44aeef5b4bb7aff5c83d15bd6f228fdcc734897f618284c7a3aaee82bf4cff72` and
`062823f8f41a07629c6010ee9b571e83adabec125984c1ee53b8f99254e465d9`.
Both manifests validate against the final tree. Cumulative current personal source admission is
190 of 4,116 current C# files.

## Behavior and assertion review

Personal review covers caller-owned construction, the single public Azure Blob vocabulary,
default and custom blob naming, endpoint/container address boundaries, create-only uploads,
bounded block staging, compression-source failure before commit, persisted-content decoding,
unsupported encoding and Azure-failure translation, container startup, injectable-time TTL
metadata, cancellation, exact-byte Azurite round trips, properties and duplicate-name collision.
No assertion-free, tautological, unawaited, skipped, wall-clock-dependent or swallowed-exception
case was found.

## Pairing, coverage and risk

The mandatory Roslyn pairing analyzer ran once against isolated directory
`/private/tmp/vsb-iteration150-pairing.n3Q4Nu`; protected and unrelated trees were not input. It
classified exactly 6 source and 7 test files and found 5 direct name pairs. Its sole unpaired file,
the internal `BlockBlobUploadStream`, is intentionally covered through the public compressed-upload
boundary: write and commit state machines have 100% coverage and block staging has 90% coverage.

Fresh native instrumentation covers 418/456 executable lines (91.6667%) and 104/120 branches
(86.6667%) across 38 methods with package complexity 65. Thirteen methods are below 80%; twelve are
single-purpose Stream accessors or unsupported-operation guards and the thirteenth is one defensive
state branch in `ThrowIfUnavailable`. No method exceeds CRAP 30. The highest CRAP value is 14 for
fully covered `ResolveBlobClient`; no risk-driven product or test change is justified.

## Local provider boundary

The local-integration requirement projection passes. All five actual Azurite cases stop in the
canonical fixture before product behavior with:

> Profile `UnitArchitecture` has invalid local test configuration.

The fixture requires endpoint settings and credentials from shared User Secrets or the canonical
runner. Those values were not present, fabricated or committed. Consequently, real Azurite
acceptance remains explicitly open; the environmental precondition is not misreported as either a
product regression or a green provider result.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| Focused unit baseline | 21 passed; 0 failed/skipped/other | 0.532 s |
| Focused final coverage | 21 passed; 0 failed/skipped/other | 0.911 s |
| Local requirement projection | 1 passed | 0.026 s |
| Local provider attempts | 5 blocked by missing profile before product behavior | — |
| Strict product Release build | 0 warnings / 0 errors | 27.79 s |
| Strict unit-owner Release build | 0 warnings / 0 errors | 21.32 s |
| Product/unit/local format verification (`warn`) | Exit 0, no differences | — |
| Native Core gate | 4,799 passed; 0 failed/skipped/other | 24.298 s |

Final unit sorted-name SHA-256 is
`a20bba368bd50b9cda068705f2d8f7024946042c1ec620c0f88299bbf7676b0e`. Coverage CTRF and
Cobertura SHA-256 values are `c8082e68d6a350262077a547b1d41276c7f6d9d43999e6943af442aff60e3df0`
and `9ff02af253b05194d2525907b50614f51375bfdebcfaafa52da91b6a86cc0475`.
The fresh Core CTRF SHA-256 is
`73ad4cd293b7a42e5a68cf26c31d4eeafb7cebde2632bd9477959890edcd2e34`, with sorted-name hash
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

The final test project and lockfile SHA-256 values are
`f50d98f8bf3a2e693b3b2dc297c9b4c85451b55fbeb96f2ba8fc89e0fd45ba72` and
`9f8393f0b774fe0ae34bf0e21cabe99cc7fd63a66c61db173fd0b34686762b01`.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration150-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-150-azure-storage-read-admission-2026-09-16`. External publication is
not pre-claimed and remains subject to destination- and payload-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and configured real external-provider acceptance remain open. This packet proves
only the current Azure Storage owner admission and its fresh local evidence.
