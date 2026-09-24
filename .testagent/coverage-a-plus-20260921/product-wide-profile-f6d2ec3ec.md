# Cumulative product coverage profile at f6d2ec3ec

## Exact-commit evidence

- Product/test commit: `f6d2ec3ec25ea50ec48b5f7a9242260e582b328d`.
  Since `2c0a8fc9b`, no `src` file changed. This commit adds two Amazon SQS
  SDK-client construction tests, their requirement mappings, and a changelog
  entry.
- The serial Release Unit/Architecture build completed with zero warnings and
  errors. Its log SHA-256 is
  `b7b0fd9933e80f50caf8e1701dd6ea7545f733d41cc09f33f7f0b558670ae8ca`.
  The Amazon SQS test DLL and all three product DLLs in the fresh report embed
  the full product/test revision.
- The serial full Unit/Architecture gate passed **10,367/10,367** with zero
  failures and skips. Its log SHA-256 is
  `92deeb70022c4b1e2f2195e5a3b706f69890deb84ceec67043778d3bc93ea6fe`.
- The fresh Amazon SQS Microsoft CodeCoverage run passed **214/214** with zero
  failures and skips. Its log SHA-256 is
  `fa3696bd8e4a01c5a8bb4a157545d83b13e727fca61eb3ec22c92bd463d24729`;
  its Cobertura SHA-256 is
  `1d626b28c961251090382d964b170c64812040eaf524e01302b32fd866fb88b2`.
- A read-only adversarial Red Team reviewed the final tests and requirement
  mappings and returned **PASS** after the first review's two issues were
  corrected.

## Cumulative result

| Measure | `f6d2ec3ec` | Previous `2c0a8fc9b` |
| --- | ---: | ---: |
| Line coverage | 84,088 / 93,507 = 89.9270% | 84,076 / 93,507 = 89.9141% |
| Conservative branch observation | 30,162 / 36,670 = 82.2525% | 30,153 / 36,670 = 82.2280% |
| Methods with CRAP > 30 | 28 / 25,994 | 30 / 25,994 |

`Connection.CreateSqsClient` and `Connection.CreateSnsClient` each improve
from 0/4 measured lines and CRAP 42 to 4/4 lines and CRAP 6. The tests use
the public default-host configuration with explicit credentials and a direct
connection with distinct SQS and SNS endpoint and signing-region settings.
They assert the types and actual configurations of both created AWS SDK
clients without making network calls.

## Merge and limits

`artifacts/coverage-a-plus-20260924-f6d2ec3ec/raw/` contains 46 parseable
reports for 32 product assemblies: 45 individually hash-verified inherited
reports and one fresh Amazon SQS report. Twelve broker fixture records are
inherited; no broker fixture ran in this iteration. Since `src` is unchanged,
the new Amazon SQS observations can merge with prior observations. The
JobSaga, Serialization, Retry, and RequestRate source overlays remain in
force, with 13 stale JobSaga reports excluded. Cobertura has no stable branch
identities, so the conservative result takes the largest covered count per
branch location. The capped-sum estimate is not used as the quality gate.

The merge is recorded in `analysis-46/summary.json` (SHA-256
`a0f334b5d74abcb3cf20452543d62a62143e97c56d289975b2d36410b7be8090`),
`analysis-46/methods.json` (`f10e8523e6904fe953bcc5d1cfafacd1d90c05c514764d24d2f05ec378394655`),
`provenance.json` (`aba7f9e3997b92ab6ddbaf2e80dd68b1ce0200eee47672e3ed91aa8406eb997d`),
and `overlay-policy.json` (`80774485f8069e51b20664d5a764aea5ee72d8129f59649dac2c839178538ea7`).

The no-explicit-credentials AWS SDK credential-chain path remains unverified
by these two tests. The next CRAP 42 risks are the three unobserved
`ConsumeObserverConverter` callbacks and the Azure Service Bus stream
conversion and session batching paths. The Microsoft `code-testing-agent`,
`coverage-analysis`, `test-gap-analysis`, `assertion-quality`, and `run-tests`
skills informed the tests, assertions, commands, and profile.

Global A+ remains open: line and branch coverage are below A+, and 28 methods
remain above CRAP 30.
