# Product-wide coverage profile at 4e7b54c22

## Exact scope and collection

- Product/test HEAD: `4e7b54c22cc1364584e7743c350acc4dbfc32c32`.
  The tracked `src`/`tests` diff was empty during collection; its SHA-256 is
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
  User-owned untracked `TestResults/` and `review/` were not included.
- Thirty-six fresh Microsoft CodeCoverage Cobertura reports under
  `artifacts/coverage-a-plus-20260924-4e7b54c22/raw/` cover all 32 product
  assemblies: 22 Unit, 13 local-provider, and one supplementary Abstractions
  run with `DOTNET_EnableAVX2=0`. Every counted test command exited
  successfully. The supplementary run passed 786/786 existing tests.
- The local-provider solution and the three separate SQL Server, RabbitMQ,
  and Azure Service Bus solutions built in Release with zero warnings and
  errors. The earlier Unit/Architecture gate at this HEAD passed
  10,261/10,261 tests with zero failures and skips.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record
  report hashes, 75 product-binary hashes, exact HEAD, and eight fixture
  records. Their SHA-256 values are respectively
  `4c1d071360887071a55e0b071d13c6a6447b1931b018b32029c73c573539e1a6`,
  `01ebf6e462c2893b1a8d6b875b4ae23d409f7ef5fb8ed1cce94a53b95eb9e997`,
  and `28d8c0fa1d110c504e0628e351c9ded41a9abdbba3921188117b5f34a45fee55`.
  All eight counted fixture records have empty `findings` lists.

## Product-wide result

| Measure | `4e7b54c22` | Previous `151bf0dc1` |
| --- | ---: | ---: |
| Line coverage | 83,843 / 93,474 = 89.6966% | 83,796 / 93,457 = 89.6626% |
| Branch observation, conservative merge | 30,005 / 36,628 = 81.9182% | 29,978 / 36,624 = 81.8534% |
| Branch observation, capped sum | 32,474 / 36,628 = 88.6589% | 32,451 / 36,624 = 88.6058% |
| Methods with CRAP > 30 | 48 / 25,971 | 52 / 25,965 |

The decrease in high-CRAP methods follows the PropertyAccessorFactory,
Azure receiver, Azure subscription validation, and Amazon SQS SNS envelope
phases. The four source/test changes and their adversarial reviews are in
their individual phase records. Aggregate changes cannot be assigned wholly
to any one phase because instrumentation and execution can vary between
complete profiles.

Cobertura gives per-line branch counts without stable branch identities.
The conservative merge takes the largest observed covered count at each
source location. The capped sum adds observations up to the largest valid
count. These are observations, not formal bounds for the merged test suite.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`.

## Excluded attempts and provenance limits

- The first provider build used a relative `CustomAfterMicrosoftCommonTargets`
  path and did not copy the CodeCoverage extension into the test output. It
  generated no counted report. The provider solution was rebuilt using an
  absolute overlay path before collection.
- An EntityFrameworkCore attempt without PostgreSQL failed configuration
  checks. An EventHubs attempt without Azurite failed to connect to Blob
  Storage. Their XML files were removed from this profile; successful runs
  under the required fixtures supplied the counted reports.
- A SQL Server attempt before its separate test solution was built did not
  start MTP tests or produce a counted report. The subsequent instrumented
  Release build and fixture run passed all 69 tests.
- The no-AVX2 report was initially emitted by the test platform below a new
  `TestResults/artifacts/...` directory because the output argument was
  relative. Only that freshly generated file was moved to `raw/no-avx2/`;
  the created empty directories were removed. The command records
  `DOTNET_EnableAVX2=0`, but Cobertura itself does not encode this setting.

## Next code areas

- JobService endpoint configuration has a complexity-44 closure despite
  28/28 covered lines. Reduce its structural complexity while preserving
  endpoint names, instance dependencies, and scheduling behavior.
- `BusCompositionStartupValidator.StartAsync` has CRAP 42.14 and 110/115
  covered lines; inspect the missed startup failure paths with behavioral
  oracles before changing it.
- Azure Service Bus `ConnectionContextFactory.CreateConnection` has CRAP
  42.08 with 33/42 covered lines. Authentication modes and invalid option
  combinations need adversarial review.
- The product-wide A+ goal remains open. The complete ranking is in
  `analysis-36/methods.json`; do not add tests solely to exercise dead code.
