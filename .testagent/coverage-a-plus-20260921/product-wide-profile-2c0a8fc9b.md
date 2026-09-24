# Cumulative product coverage profile at 2c0a8fc9b

## Exact-commit evidence

- Product/test commit: `2c0a8fc9be7925071257b8499ee5227691b368d5`.
  Since `8adf57e76`, no `src` file changed. The commit adds
  `CancellationAfterSave_RollsBackBusinessAndConsumedFenceWithoutChargingAnAttemptAsync`,
  its requirement mapping, and a changelog entry.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors. Its log SHA-256 is
  `d5dc1742d6d6784f8a3e4b82a3de11ce5752f5fe34b561e9128fcf2a60b4fad0`.
  The EF Core test DLL and all nine product DLLs in the fresh report embed
  the full product/test revision.
- The serial full Unit/Architecture gate passed **10,365/10,365** with zero
  failures and skips. Its log SHA-256 is
  `077441fcc68c8cf672b871447d2d882e85a105b130d94a65fc2b4ae8e0a6057c`.
- The fresh EF Core Microsoft CodeCoverage run passed **276/276** with zero
  failures and skips. Its log SHA-256 is
  `dc528eb514b6ff2592b0c316f93d6c8fc94785727f488912ca59745bb152dbc8`;
  its Cobertura SHA-256 is
  `c797ec5498cd4da313252c51446da4b827f6cc2828a0428c39e99091b591f6eb`.
- A read-only adversarial Red Team reviewed the final test and requirement
  mapping and returned **PASS**.

## Cumulative result

| Measure | `2c0a8fc9b` | Previous `8adf57e76` |
| --- | ---: | ---: |
| Line coverage | 84,076 / 93,507 = 89.9141% | 84,071 / 93,507 = 89.9088% |
| Conservative branch observation | 30,153 / 36,670 = 82.2280% | 30,150 / 36,670 = 82.2200% |
| Methods with CRAP > 30 | 30 / 25,994 | 30 / 25,994 |

`EntityFrameworkReliableInboxContextFactory.SendAsync` improves from 87/101
lines and CRAP 39.45 to 91/101 lines and CRAP 37.26. The new test uses real
SQLite transactions and the registered EF inbox factory. Inside the open
transaction, independent `AsNoTracking` reads prove that business data and
the consumed fence have been flushed. Cancellation then must remove both
records and clear tracked state. The same message and consumer identifiers
subsequently commit with one attempt, no failure evidence, and only the new
business value. This checks transaction behavior rather than method entry.

## Merge and limits

`artifacts/coverage-a-plus-20260924-2c0a8fc9b/raw/` contains 45 parseable
reports for 32 product assemblies: 44 individually hash-verified inherited
reports and one fresh EF Core report. Twelve broker fixture records are
inherited; no broker fixture ran in this iteration. Since `src` is unchanged,
the new EF Core observations can merge with prior observations. The JobSaga,
Serialization, Retry, and RequestRate source overlays remain in force; the
fresh EF Core report adds one JobSaga exclusion, yielding 13 stale JobSaga
reports. Cobertura has no stable branch identities, so the conservative
result takes the largest covered count per branch location. The capped-sum
estimate is not used as the quality gate.

The merge is recorded in `analysis-45/summary.json` (SHA-256
`4982e6cc1b8e9a3482222144b86221a3f75fa691812bf1e3b8dc2c2e275757a8`),
`analysis-45/methods.json` (`b8b7daad10b11f5517d585fa6627c739b055ed66b335fa8b380e123331c14860`),
`provenance.json` (`cccf92d3bf27d66de312b5298d96d9b02008f1e3ebaeef042437577e34dcab5f`),
and `overlay-policy.json` (`b453b6785a7bad414eb9ee1ff62ec71db8e7c7c183714ef45230ba5fde8ce873`).

Top remaining CRAP risks remain the three unobserved
`ConsumeObserverConverter` callbacks, two Amazon SQS client-creation methods,
and Azure Service Bus stream conversion and session batching paths, each at
CRAP 42. The Microsoft `code-testing-agent`, `coverage-analysis`,
`test-gap-analysis`, `assertion-quality`, and `run-tests` skills informed the
test, its assertions, commands, and profile.

Global A+ remains open: line and branch coverage are below A+, and 30 methods
remain above CRAP 30.
