# Product-wide coverage profile at source/test commit a95505227

## Scope and provenance

- Exact source/test commit: `a955052272b744ceb44333a51035ac9ce4f66e3d`.
  The tracked `src`/`tests` diff was empty at aggregation; its SHA-256 was
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- Microsoft CodeCoverage used `tools/ci/coverage.settings.xml`. All 36 fresh,
  parseable Cobertura reports are under
  `artifacts/coverage-a-plus-20260922-a95505227/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one Abstractions run with `DOTNET_EnableAVX2=0`.
  `analysis-36/summary.json` records every report path and SHA-256. All 32
  loadable source assemblies were observed; none were missing or unexpected.
  `analysis-36/methods.json` contains every measured method, including all 91
  with CRAP above 30.
- The 22 Unit/Infrastructure modules passed 9,577/9,577 tests; the 13 provider
  modules passed 545/545; the supplementary no-AVX2 run passed 759/759.
  Every counted run had zero failures and skips. Four canonical fixture runs
  (`vicione-f695c7726e2c`, `vicione-3b7f6549a60e`,
  `vicione-3259519979e5`, `vicione-5c15cf8b8d9f`) have empty findings.
- The artifact-local `provenance.json` captures the source/test HEAD and diff,
  all 36 report and test-binary hashes, 57 source-binary entries for the 32
  projects across two SDK output roots, collection-log hashes, and the four
  fixture-findings hashes. Hashes record the post-build state; by themselves
  they do not prove how a binary was produced.

## Build and collection record

- Locked restore and Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`
  used `--artifacts-path artifacts/coverage-a-plus-20260922-a95505227/unit-sdk`.
  The corrected runs passed the **absolute**
  `unit-additional-coverage.targets` path through
  `CustomAfterMicrosoftCommonTargets`; the Release build finished with zero
  warnings and errors. The first 18 projects ran through
  `run-unit-coverage.sh`; the remaining four ran through
  `run-unit-additional-coverage.sh` after the correction.
- Locked restore and Release build of
  `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` used the separate
  `provider-sdk` output root and the **absolute**
  `local-provider-coverage.targets` path. The corrected Release build finished
  with zero warnings and errors. The ten shared-fixture projects ran through
  `run-broad-provider-coverage.sh` inside the canonical six-broker runner,
  including `--allow-broker-outage activemq`. The remaining three ran through
  `run-remaining-provider-coverage.sh`, which restores and builds each profile
  before running its own canonical fixture; those builds also reported zero
  warnings and errors.
- The supplementary Abstractions command was run with
  `DOTNET_EnableAVX2=0`, `--no-build --no-restore`, the `unit-sdk` output root,
  and an **absolute** `--coverage-output` path in `raw/portability/`. Its
  759-test result and report destination are recorded in
  `no-avx2-abstractions-2.log`. The executed tool command in this work
  session included the environment variable; the artifact log and XML do
  not independently prove that setting. The report covers the scalar
  formatter path.
- An initial provider attempt returned zero discovered tests because a
  relative overlay path did not add the coverage package to its assets. An
  initial Benchmark attempt encountered the same error. Both returned a
  nonzero test-run exit code and have **no** report in `raw/`. Their logs are
  retained as `broad-provider-coverage.log` and `unit-coverage.log`; the
  latter also contains the 18 successful Unit blocks. An initial no-AVX2
  attempt passed its tests but placed its report beneath `TestResults/`
  because its output path was relative. That report is also outside `raw/`;
  only the corrected second run is counted. No result from these attempts
  contributes to the 36-report aggregate.
- Architecture tests remain outside the coverage aggregate because
  instrumentation changes an assembly-ownership assertion. The last complete
  Unit/Architecture gate on these exact source/test bytes passed
  10,022/10,022; see `recurring-scheduler-dispatch-phase.md`.

The console records for the initial solution restores and builds were not
archived as separate files. The commands, exit codes, and zero-warning build
results were observed in this work session; the artifact-local manifest
provides the post-build binary hashes. This is the limit of binary-to-HEAD
provenance for this measurement.

## Product-wide result

| Measure | Current `a95505227` | Previous `4488b29fe` |
| --- | ---: | ---: |
| Line coverage | 83,256 / 93,283 = 89.2510% | 83,067 / 93,200 = 89.1277% |
| Branch coverage, conservative lower bound | 29,674 / 36,695 = 80.8666% | 29,565 / 36,649 = 80.6707% |
| Branch coverage, capped upper bound | 32,209 / 36,695 = 87.7749% | 32,111 / 36,649 = 87.6177% |
| Methods with CRAP > 30 | 91 / 25,892 | 97 / 25,886 |

Cobertura reports per-line branch counts without branch identities. The
aggregate merges unique source class/line counts across suites. The lower
bound takes the maximum observed covered count; the upper bound caps the
sum. Neither is an exact global branch rate. Source and test additions also
changed the denominators, so the rate difference is descriptive rather than
a fixed-denominator causal attribution.

The Microsoft `coverage-analysis` skill informed collection and CRAP review.
Its PowerShell scripts could not run because `pwsh` is unavailable in this
environment. The repository aggregator applies the documented
`complexity² × (1 − method line coverage)³ + complexity` formula, and the
independent review recalculated its totals from the Cobertura files.

## Highest current CRAP risks

| CRAP | Method | Covered source lines |
| ---: | --- | ---: |
| 156 | Abstractions `DictionaryExtensions.SetValue` | 0/11 |
| 110 | Core `TimeoutActivityContextProxy.NotifyFaultedAsync` | 0/11 |
| 104 | Job Service `JobStateMachine` constructor | 167/167 |
| 96 | RabbitMQ `ConnectionContextFactory.CreateConnectionAsync` | 19/38 |
| 94.98 | RabbitMQ send transport `SendAsync` | 79/85 |
| 82.54 | Core `CircuitBreakerSettings.Validate` iterator | 8/15 |
| 72 | Azure Service Bus `QueueClientContext.EntityPath` | 0/2 |
| 72 | Azure Service Bus topology `LogResult` | 0/6 |
| 72 | RabbitMQ registration `CreateBus` closure | 0/13 |
| 72 | RabbitMQ topology `LogResult` | 0/7 |

The four largest uncovered-line owners are Core (3,840), Abstractions
(1,121), Azure Service Bus (863), and RabbitMQ (713). The earlier PostgreSQL
maintenance closure is now 8/8 lines with CRAP 12; dynamic request-rate
change is 53/56 with CRAP 24.09; the selected SQS visibility validator is
16/16 with CRAP 22; and the two selected recurring-converter methods are
below CRAP 30. The Job Service constructor remains above 30 despite full
coverage because its measured complexity is 104. `SetValue` belongs to an
internal helper. Its only observed product-source caller is `SetValues`,
which has no observed caller elsewhere in `src`. Reachability and contract
ownership need review; a test added only to raise coverage would not satisfy
the behavioral test rule.

An independent read-only adversarial review reproduced all 36 report hashes,
the 32-assembly scope, deduplicated line and branch totals, 25,892 methods,
91 CRAP risks, test counts, four empty fixture findings, and all nine broker
log hashes. Its concrete finding about the initial no-AVX2 output path was
corrected by a new run with an absolute path. The final quantitative review
returned PASS. Global A+ remains open.
