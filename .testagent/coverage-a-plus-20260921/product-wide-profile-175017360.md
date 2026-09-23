# Product-wide coverage profile at 175017360

## Scope and evidence

- Source and test commit: `17501736000f950b70c176cc815a7a70aa36f8bf`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- The 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-175017360/raw/` comprise 22
  Unit/Infrastructure reports, 13 local-provider reports, and one supplementary
  Abstractions run with `DOTNET_EnableAVX2=0`. Every counted test command exited
  successfully. The complete serial Unit/Architecture gate passed
  10,218/10,218 tests with no failures or skips after a warning-free Release
  build. Architecture tests are outside the instrumented aggregate because
  instrumentation affects their assembly-ownership assertion.
- The reports contain all 32 product assemblies. Report hashes, 77 product
  binary hashes, the exact HEAD, and four fixture records are in the artifact
  `provenance.json`; method details are in `analysis-36/methods.json`. All four
  counted fixture runs have empty findings lists. The multi-broker fixture
  used the required ActiveMQ outage control; SQL Server, RabbitMQ, and Azure
  Service Bus used separate fixtures.
- An initial combined build/test attempt exited during its build without a
  useful diagnostic. A separate Release build succeeded with zero
  warnings/errors. The subsequent sandboxed no-build test command failed
  before executing product tests because the sandbox denied the .NET test
  runner's socket; the approved no-build serial gate then passed. A sandboxed
  local-provider restore was canceled while waiting for
  its network source; a separate approved locked restore and warning-free build
  produced the counted `provider-sdk-net` binaries. No failed or incomplete
  test or coverage report is under `raw/`.
- Coverage used `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
  Binary and report hashes describe the collected files but do not alone
  prove a report-to-executed-binary or build-to-commit chain. Exact-HEAD
  attribution also relies on the observed build/test sequence and unchanged
  tracked source/test content.

## Product-wide result

| Measure | Current `175017360` | Previous `90819758b` |
| --- | ---: | ---: |
| Line coverage | 83,660 / 93,417 = 89.5554% | 83,649 / 93,414 = 89.5465% |
| Branch observation, conservative merge | 29,919 / 36,684 = 81.5587% | 29,918 / 36,688 = 81.5471% |
| Branch observation, capped sum | 32,402 / 36,684 = 88.3273% | 32,401 / 36,688 = 88.3150% |
| Methods with CRAP > 30 | 66 / 25,943 | 66 / 25,943 |

`ServiceBusHostConfigurator.ParseEndpoint` changed from 28/37 measured lines,
complexity 36, CRAP 54.65 to 35/40 lines, complexity 32, CRAP 34.00. The
additional assertions protect malformed connection-string behavior before
and after a valid endpoint; the product-wide A+ target remains open.

Cobertura supplies per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. At
`MessagePackMessageSerializer.cs:194`, reports disagree about the valid branch
count, so these observations are not formal bounds on exact merged branch
coverage. CRAP uses `complexity² × (1 − method line coverage)³ + complexity`.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. Line and
branch coverage and 66 method CRAP values remain below the requested A+ target.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72 and 0/1 measured
  lines. Confirm a reachable public failure path before adding a test;
  synthetic reflection state would not establish product behavior.
- `CronExpression.StoreExpressionGeneralValue`: CRAP 66 despite full measured
  line coverage; behavior-led simplification is needed.
- JobService partition-key and correlation registration: CRAP 62 each despite
  full measured line coverage. Existing partition-key tests check all 31
  contracts against distinct owner IDs; avoid moving calls solely for a score.
- Azure Service Bus receiver exception handling and the endpoint parser:
  uncovered failure paths and CRAP 34 in `ParseEndpoint` still require work.

The exact method ordering and source locations are in `analysis-36/methods.json`.
