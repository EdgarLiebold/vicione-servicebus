# Iteration 149 — Amazon S3 read admission

## Scope and result

This bounded packet personally rereads all current C# files and comments in
`ViciOne.ServiceBus.AmazonS3`, its project metadata, both directly owning test projects, their
support fixture and bound requirement projections. The owner has 4 C# files / 461 lines; its two
test owners contain 6 relevant C# files / 797 lines.

No current correctness, lifetime, cancellation, naming, namespace, placement, dependency,
comment or public-API defect was reproduced. The admission changes no product or C# test code. It
adds the centrally pinned repository-standard `Microsoft.Testing.Extensions.CodeCoverage` package
to the unit owner and records only that package's deterministic direct/transitive lock entries.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 4 / 461 | `1db978a2ebd2deeb127cb85f18a23ceb7eae5687c31899f414f6d66c9dc0827f` | `44aeef5b4bb7aff5c83d15bd6f228fdcc734897f618284c7a3aaee82bf4cff72` |
| Owning test/support | 6 / 797 | `04c1b82c0e48cd1ca74e5c996e15b373d75143717d22bda31d6590b13e1090aa` | `062823f8f41a07629c6010ee9b571e83adabec125984c1ee53b8f99254e465d9` |

The chains extend Iteration-148 chain hashes
`860263f5d456c2d4ae9f60522175735a19d8ea23563ba8b2452ee962dbb863d9` and
`9476b121136659b1a331b7dc42367f0cded753be3dc66e6cc8a0f231d05cc26a`.
Both manifests validate against the final tree. Cumulative current personal source admission is
184 of 4,116 current C# files.

## Behavior, assertions and pairing

Personal review covers bucket-name and TTL boundaries, safe object-key/URI construction, caller
cancellation, startup bucket creation and fail-closed readiness, concurrent own-bucket creation,
configured region, exact-byte storage round-trip, and canonical reconciliation of only the owned
lifecycle rule while preserving foreign rules. No assertion-free, tautological, unawaited,
skipped, wall-clock-dependent or swallowed-exception case was found.

A null lifecycle-expiration option explicitly means that no lifecycle rule is managed. Removing a
previously present owned rule would be destructive and lacks a contrary requirement, so the
existing non-management behavior was preserved.

The mandatory Roslyn pairing analyzer ran once against isolated directory
`/private/tmp/vsb-iteration149-pairing.N468Db`; protected and unrelated trees were not input. It
classified exactly 4 source and 6 test files and paired all 4 sources to owner tests.

## Coverage and risk

Fresh native instrumentation covers 400/428 executable lines (93.4579%) and 232/272 branches
(85.2941%) across 34 methods with package complexity 140. Two methods are below 80% line coverage;
none exceeds CRAP 30. The highest CRAP values are `ValidateBucketName` 30 at full coverage,
`ParseObjectKey` 20.0814 at 16/17 lines, and the highest generated `MoveNext` 20.0079 at 36/37
lines. No risk-driven test addition is justified by the current evidence.

## Local provider boundary

The local-integration requirement projection passes. All four actual LocalStack tests stop in the
canonical fixture before product behavior with:

> Profile `UnitArchitecture` has invalid local test configuration.

The fixture requires endpoint settings and credentials from shared User Secrets or the canonical
runner. Those values were not present, fabricated or committed. Consequently, real Amazon-S3-
compatible provider acceptance remains explicitly open; the environmental precondition is not
misreported as either a product regression or a green provider result.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| Focused unit baseline | 11 passed; 0 failed/skipped/other | 1.044 s |
| Focused final coverage | 11 passed; 0 failed/skipped/other | 1.049 s |
| Local requirement projection | 1 passed | 0.032 s |
| Local provider attempts | 4 blocked by missing profile before product behavior | — |
| Strict product Release build | 0 warnings / 0 errors | 21.95 s |
| Strict unit-owner Release build | 0 warnings / 0 errors | 8.24 s |
| Product/unit/local format verification (`warn`) | Exit 0, no differences | — |
| Native Core gate | 4,799 passed; 0 failed/skipped/other | 30.039 s |

Final unit sorted-name SHA-256 is
`02027412f56719e550f9799461e8f69d042ef018ae0c22d5dc9a13d6894a004e`. Coverage CTRF and
Cobertura SHA-256 values are `d3d26617fff19c251d994b8027527f7613e57f0c8c8cc7474d9ff57d71132998`
and `443bbcb9964cebd04ab72aa78d94894688d72c9e3d26a906667ac7e919b57f8c`.
The fresh Core CTRF SHA-256 is
`36419731f191513ec366f67fd01955da3c9efc3d88ca585d313d47ed3ac86b39`, with sorted-name hash
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

The final test project and lockfile SHA-256 values are
`3808c5366244f1ca5f258b99d58581343495e773ac48dde7ea5185244c6260ed` and
`46df372db0f9225e84a1a7b4e7ba7e925a61eab95824334a2cf4b8a122914a4b`.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration149-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-149-amazons3-read-admission-2026-09-16`. External publication is not
pre-claimed and remains subject to destination- and payload-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and configured real external-provider acceptance remain open. This packet proves
only the current Amazon S3 owner admission and its fresh local evidence.
