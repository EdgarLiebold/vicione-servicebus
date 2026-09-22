# Product-wide coverage profile at source/test commit f7d924f94

## Scope and provenance

- Exact source/test commit: `f7d924f9477bc95bfc46bda67e3d545b8721066b`. The tracked `src`/`tests`
  diff was empty at aggregation; its SHA-256 was
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- Microsoft CodeCoverage used `tools/ci/coverage.settings.xml`. All 36 fresh, parseable Cobertura
  reports are under `artifacts/coverage-a-plus-20260922-f7d924f94/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one supplementary Abstractions run. The live tool protocol recorded
  `DOTNET_EnableAVX2=0` for that supplementary run; its archived log proves the 759/759 result and
  report path, but does not repeat the command or environment variable.
- The Unit/Infrastructure modules passed 9,602/9,602 tests. Provider modules passed 545/545 and the
  supplementary no-AVX2 run passed 759/759. Every counted run had zero failures and skips.
- The four canonical fixture runs (`vicione-98a1cb148b5c`, `vicione-501585e55b48`,
  `vicione-ef486d505a7f`, and `vicione-ecac27b3a047`) have empty findings. Their nine broker-log
  hashes and every report/test-binary hash are recorded by the artifact-local `provenance.json`.
- The aggregate observed every one of the 32 product source assemblies, with no missing or
  unexpected assembly. `analysis-36/methods.json` contains all 25,902 measured methods.

## Build and collection record

- Locked restores and Release builds used separate `unit-sdk` and `provider-sdk` output roots. The
  live tool protocol recorded zero warnings and errors for the Unit and provider solution builds;
  their archived collection logs begin with the subsequent test runs and do not independently
  preserve those two build results.
- The ten shared-fixture providers ran through the canonical six-service fixture, including the
  ActiveMQ outage relay. SQL Server, RabbitMQ, and Azure Service Bus each ran through a separate
  canonical fixture after their own locked restore and zero-warning build.
- The 36 reports are new outputs in a new directory. None of the previous profile's XML files was
  used by this aggregate. The report, binary, collection-log, and fixture hashes are in
  `artifacts/coverage-a-plus-20260922-f7d924f94/provenance.json`.
- Those hashes prove the exact files used by the aggregate. They do not by themselves prove that
  each hashed binary was built from `f7d924f94`; the empty tracked source/test diff and the live
  isolated-build protocol provide the remaining provenance evidence.
- Architecture tests remain outside the coverage aggregate because instrumentation changes an
  assembly-ownership assertion. The current source/test bytes passed the complete
  Unit/Architecture gate 10,047/10,047 during the RabbitMQ send phase.

## Product-wide result

| Measure | Current `f7d924f94` | Previous `a95505227` |
| --- | ---: | ---: |
| Line coverage | 83,286 / 93,309 = 89.2583% | 83,256 / 93,283 = 89.2510% |
| Branch coverage, conservative lower bound | 29,698 / 36,695 = 80.9320% | 29,674 / 36,695 = 80.8666% |
| Branch coverage, capped upper bound | 32,135 / 36,695 = 87.5732% | 32,209 / 36,695 = 87.7749% |
| Methods with CRAP above 30 | 89 / 25,902 | 91 / 25,892 |

Cobertura reports counts per source class and line without branch identities. The lower bound takes
the maximum covered count observed for a line; the upper bound caps the sum from all reports. The
upper bound can decrease when a fresh execution takes a different path even while the lower bound
improves. Source changes also added 26 valid lines and ten measured methods, so this is not a fixed
denominator comparison.

The Microsoft `coverage-analysis` skill informed collection and CRAP review. Its PowerShell scripts
cannot run because `pwsh` is unavailable. The repository aggregator uses
`complexity² × (1 − method-line-coverage)³ + complexity` with Cobertura complexity.

## Highest current CRAP risks

| CRAP | Method | Covered source lines |
| ---: | --- | ---: |
| 156 | Abstractions `DictionaryExtensions.SetValue` | 0/11 |
| 110 | Core `TimeoutActivityContextProxy.NotifyFaultedAsync` | 0/11 |
| 104 | Job Service `JobStateMachine` constructor | 167/167 |
| 96 | RabbitMQ `ConnectionContextFactory.CreateConnectionAsync` | 19/38 |
| 82.54 | Core `CircuitBreakerSettings.Validate` iterator | 8/15 |
| 72 | Azure Service Bus `QueueClientContext.EntityPath` | 0/2 |
| 72 | Azure Service Bus topology `LogResult` | 0/6 |
| 72 | RabbitMQ registration `CreateBus` closure | 0/13 |
| 72 | RabbitMQ topology `LogResult` | 0/7 |

The RabbitMQ send state machine is now 34/34 lines, 18/20 reported branches, complexity 20, and
CRAP 20 in the Unit report, so it no longer appears above the risk threshold. The product-wide A+
goal remains open: line and branch coverage are below A+, and 89 methods remain above CRAP 30.

Independent read-only adversarial review reproduced the report count, assembly set, test totals,
aggregate metrics, hashes, and empty fixture findings. It returned PASS after the evidence limits
above were made explicit.
