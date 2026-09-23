# Product-wide coverage profile at 6a3689d33

## Reproducible evidence

- Product and test source: `6a3689d33`; the evidence-only HEAD at aggregation was
  `929e454087411c2130449babb88a7266944fff1d`. The tracked `src`/`tests`
  diff was empty (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- All 36 fresh Cobertura reports are under
  `artifacts/coverage-a-plus-20260923-6a3689d33/raw/`: 22 Unit and
  Infrastructure runs, 13 local-provider runs, and one supplementary
  Abstractions run with `DOTNET_EnableAVX2=0`. They cover all 32 product
  assemblies. The five successful run logs show 36/36 test commands passing,
  11,094 executed tests across those commands, zero failures, and zero skips.
  The supplementary Abstractions run repeats 768 tests already represented
  by its ordinary run; 11,094 is an execution count, not a unique-test count.
- `analysis-36/summary.json` and `methods.json` contain the merged counts and
  method risk data. `provenance.json` hashes the 36 reports, eight log files,
  two analysis files, collection scripts, 77 product binaries, and four
  fixture records. All four fixture
  findings lists are empty. The broad six-broker fixture exercised the
  required ActiveMQ outage control; SQL Server, RabbitMQ, and Azure Service
  Bus used separate fixtures. The archived `provider-build.log` and the three
  provider builds in `remaining-provider.log` show zero warnings and errors.
- The first supplementary Abstractions attempt failed before running tests
  because the sandbox denied the .NET test host's local pipe. Its separate
  `no-avx2-abstractions.log` is retained and hashed; the successful escalated
  retry is `no-avx2-abstractions-escalated.log` and alone contributes a report.
  The executed command set `DOTNET_EnableAVX2=0`; the console log and XML do
  not independently attest that environment setting.
- `provider-restore.log` is empty because the successful quiet restore wrote
  no console output. File hashes preserve the collected evidence but alone
  do not prove the executed-binary-to-report or build-to-commit chain. That
  attribution also relies on the observed ordered builds and test runs and
  the unchanged tracked source and test files.
- The full Release build and uninstrumented Unit/Architecture gate were
  separately archived for product/test commit `6a3689d33`: zero build
  warnings/errors and 10,226/10,226 tests. See
  `sns-subscription-all-attributes-phase.md` and its hashed artifacts.

## Measured product-wide result

| Measure | Current `6a3689d33` | Previous `5da15ce2e` |
| --- | ---: | ---: |
| Line coverage | 83,702 / 93,411 = 89.6061% | 83,684 / 93,417 = 89.5811% |
| Branch observation, conservative merge | 29,939 / 36,668 = 81.6488% | 29,926 / 36,684 = 81.5778% |
| Branch observation, capped sum | 32,419 / 36,668 = 88.4122% | 32,409 / 36,684 = 88.3464% |
| Methods with CRAP > 30 | 64 / 25,943 | 65 / 25,943 |

The SNS subscription change raised merged coverage of
`AmazonSqsClientContext.CreateQueueSubscriptionAsync` to 43/44 measured lines;
`ChangedSubscriptionAttributes` has 5/5 lines, complexity 6, and CRAP 6.
The changed source also changed the instrumented line and branch denominators,
so percentage differences alone do not describe newly executed paths.

Cobertura supplies branch counts by source line without stable branch
identities. The aggregator merges unique source lines across reports. The
conservative branch observation takes the largest covered count at each
location; the capped sum adds covered counts up to the largest valid count.
These are observations, not formal bounds on exact combined branch coverage;
reports disagree on some valid counts. CRAP is computed as
`complexity² × (1 − method line coverage)³ + complexity`. The Microsoft
`coverage-analysis` skill guided collection and risk review. Its PowerShell
scripts could not run because `pwsh` is unavailable; the artifact-local Python
analyzer applies the documented formula.

The product-wide A+ target remains open in line coverage, branch coverage,
and CRAP. The next work should select behaviorally meaningful gaps from
`analysis-36/methods.json`, add tests that detect real defects or simplify
well-covered high-complexity methods, and repeat this full profile after each
material source/test change.

An independent, read-only adversarial review reaggregated the raw XML and
verified the figures, report/log/script/binary hashes, empty fixture findings,
and stated attribution limits. It returned PASS after the evidence wording was
corrected.
