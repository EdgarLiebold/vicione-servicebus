# Cumulative product coverage profile at 8adf57e76

## Exact-commit evidence

- Product/test commit: `8adf57e7638c6606c581e44fcc3e87e51e689426`.
  Since `6b52c9ee1`, no `src` file changed. The commit adds the
  `TypedSender_RelativeDestinationCannotReserveAnIdempotencyKeyAsync`
  integration test, its requirement mapping, and a changelog entry.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors. Its log SHA-256 is
  `1989d3628745c390c4b8b8ac8599cbf2ae0d7d138ff8d67feccc0406d5f21b16`.
  The Core test DLL and all ten fresh coverage-report assembly DLLs embed the
  full product/test revision.
- The serial full Unit/Architecture gate passed **10,364/10,364** with zero
  failures and skips. Its log SHA-256 is
  `a3fc60ae0ae21ffc3d88ed223551fe8ea2f737dd2b7f66d903e9228ba9ce9436`.
- The fresh Core Microsoft CodeCoverage run passed **6,393/6,393** with zero
  failures and skips. Its log SHA-256 is
  `d2f82f1efeb571edc23a6443e6dda2758447957b8f4b897ed9aaa54008045433`;
  its Cobertura SHA-256 is
  `332e2ad2b745906c81c4b085e4a1cac1d2f9b2ca4f6f9499e20051570ed435d0`.
- A read-only adversarial Red Team found an insufficient diagnostic oracle.
  The test now asserts both the exact `destinationAddress` parameter and the
  ordinal `absolute URI` diagnostic. The final review returned **PASS**.

## Cumulative result

| Measure | `8adf57e76` | Previous `6b52c9ee1` |
| --- | ---: | ---: |
| Line coverage | 84,071 / 93,507 = 89.9088% | 84,070 / 93,507 = 89.9077% |
| Conservative branch observation | 30,150 / 36,670 = 82.2200% | 30,149 / 36,670 = 82.2171% |
| Methods with CRAP > 30 | 30 / 25,994 | 30 / 25,994 |

`TypedDurableSender.SendAsync` improves from 62/70 lines and CRAP 37.93 to
63/70 lines and CRAP 37.30. The test exercises the public typed facade with
the configured InMemory transport and reliable store. It proves that a
relative destination raises the expected argument error without creating an
outbox record or reserving the idempotency key; the same key then admits one
claimable intent at the absolute destination. It is a product boundary test,
not a coverage-only invocation.

## Merge and limits

`artifacts/coverage-a-plus-20260924-8adf57e76/raw/` contains 44 parseable
reports for 32 product assemblies: 43 individually hash-verified inherited
reports and one fresh Core report. Twelve broker fixture records are inherited;
no broker fixture ran in this iteration. Since `src` is unchanged, the fresh
Core observations can be merged with prior observations. The existing JobSaga,
Serialization, Retry, and RequestRate source overlays remain in force; the
fresh Core report adds one JobSaga exclusion, yielding 12 stale JobSaga
reports. Cobertura has no stable branch identities, so the conservative result
takes the largest covered count per branch location. The capped-sum estimate is
not used as the quality gate.

The merge is recorded in `analysis-44/summary.json` (SHA-256
`11f92d55013b469412e646dbb74ed60590db0fe472a3143a1d0f41e99a0c5b17`),
`analysis-44/methods.json` (`f357e3476c0379c9fdd6ff022e9d6c4f304bac7bbfbf2e4960f36a36a1decf36`),
`provenance.json` (`227f4c22e1f1c8e349e3de3cdaba6b920f9712d03cf3d4328b9a346bab676208`),
and `overlay-policy.json` (`2696dcde8fcf6d15f44b8844bb269389cec3ac30115b93137cce2beb07c1f252`).

Top remaining risks are three unobserved `ConsumeObserverConverter` callbacks,
two unobserved Amazon SQS client-creation methods, and unobserved Azure Service
Bus stream conversion and session batching paths, each at CRAP 42. The
`ConsumeObserverConverter` and stream converter currently have no production
call sites, so a direct execution test would not by itself establish product
value. The Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`,
and `test-gap-analysis` skills guided this focused test and profile.

Global A+ remains open: line and branch coverage are below A+, and 30 methods
remain above CRAP 30.
