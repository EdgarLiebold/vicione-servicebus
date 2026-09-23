# Product-wide coverage profile at source/test commit ac363722c

## Scope and provenance

- Exact source/test commit: `ac363722cafe0fba62e60213b2784270415903f4`. The tracked `src`/`tests`
  diff was empty at aggregation; its SHA-256 was
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- Microsoft CodeCoverage used `tools/ci/coverage.settings.xml`. All 36 fresh, parseable Cobertura
  reports are under `artifacts/coverage-a-plus-20260923-ac363722c/raw/`: 22 Unit/Infrastructure,
  13 provider, and one supplementary Abstractions report with `DOTNET_EnableAVX2=0`.
- Unit/Infrastructure passed 9,711/9,711 tests. Provider projects passed 545/545 and the
  supplementary no-AVX2 Abstractions run passed 759/759. Every counted run had zero failures and
  skips.
- The four canonical fixture runs (`vicione-c6cf2bd5436a`, `vicione-6cc3eb68be5e`,
  `vicione-cd1bd90c3865`, and `vicione-b9224ac56d59`) have empty findings. Nine broker logs,
  every report, and the collected source binaries are hashed in the artifact-local
  `provenance.json`.
- The aggregate observed all 32 product source assemblies. `analysis-36/methods.json` contains
  25,917 measured methods.

## Build and collection record

- Locked restores and Release builds used separate `unit-sdk` and `provider-sdk` output roots.
  The final Unit and provider builds completed with zero warnings and errors.
- Four Unit projects and eleven provider projects did not directly reference the centrally pinned
  CodeCoverage package. Collection used an artifact-local MSBuild overlay with the repository's
  central `Microsoft.Testing.Extensions.CodeCoverage` version 18.10.0 and project-specific
  artifact-local lock files. Tracked package lock files were unchanged. Failed setup attempts were
  excluded from the aggregate.
- The ten shared-fixture providers ran through the canonical six-service fixture. SQL Server,
  RabbitMQ, and Azure Service Bus each ran through their separate canonical fixture after locked
  restore and a zero-warning Release build.
- The 36 reports are new outputs in a new directory. Report, binary, fixture, and broker-log hashes
  are in `artifacts/coverage-a-plus-20260923-ac363722c/provenance.json`.
- Architecture tests remain outside the coverage aggregate because instrumentation changes an
  assembly-ownership assertion. The same source/test bytes passed the exact complete
  Unit/Architecture gate 10,156/10,156 during the preceding focused phase.

## Product-wide result

| Measure | Current `ac363722c` | Previously published `f7d924f94` |
| --- | ---: | ---: |
| Line coverage | 83,389 / 93,236 = 89.4386% | 83,286 / 93,309 = 89.2583% |
| Branch coverage, conservative lower bound | 29,798 / 36,715 = 81.1603% | 29,698 / 36,695 = 80.9320% |
| Branch coverage, capped upper bound | 32,315 / 36,715 = 88.0158% | 32,135 / 36,695 = 87.5732% |
| Methods with CRAP above 30 | 83 / 25,917 | 89 / 25,902 |

Cobertura supplies covered branch counts per class and line without stable branch identities. The
lower bound takes the maximum covered count observed for each class line; the upper bound caps the
sum from all reports. The current aggregator keeps distinct compiler-generated and declaring
classes on the same physical source line separate. Method identity excludes the unstable first
sequence-point line and merges that line only as display metadata.

The earlier profile used source and line alone for branch identity and included the first sequence
point in method identity. Its published values are retained as historical evidence and are only a
descriptive comparison. Source and denominator changes also prevent a fixed-denominator trend
claim.

The Microsoft `coverage-analysis` and `crap-score` skills informed collection and review. Their
PowerShell scripts cannot run because `pwsh` is unavailable. The repository aggregator uses
`complexity² × (1 − method-line-coverage)³ + complexity` with Cobertura complexity.

## Closed recent hotspots

- The dead Abstractions dictionary mutation overloads are absent.
- The Core timeout activity family has maximum CRAP 16.
- The Job Service state-machine constructor has CRAP 1.
- The RabbitMQ connection-context family has maximum CRAP 20.
- The Core circuit-breaker validation iterator has CRAP 26.
- The RabbitMQ registration-options family has maximum CRAP 8.

## Remaining CRAP distribution

| Assembly | Methods above CRAP 30 |
| --- | ---: |
| Core `ViciOne.ServiceBus` | 24 |
| Abstractions | 15 |
| Azure Service Bus | 15 |
| Job Service | 10 |
| Amazon SQS | 6 |
| RabbitMQ | 3 |
| Sagas | 3 |
| Entity Framework Core | 3 |
| State Machine Visualizer | 1 |
| SQL Server | 1 |
| Analyzer Code Fixes | 1 |
| Testing | 1 |

## Highest current CRAP risks

| CRAP | Method | Covered source lines |
| ---: | --- | ---: |
| 72 | Core `MessageScheduler.ScheduleSendAsync` | 0/9 |
| 72 | Core scheduler converter `ScheduleSendAsync` state machine | 0/10 |
| 72 | Abstractions `ConsumerDefinition.ConcurrencyPolicy` setter | 0/9 |
| 72 | Abstractions `PropertyAccessorFactory.IsCompilationFailure` | 0/1 |
| 72 | Azure Service Bus `QueueClientContext.EntityPath` | 0/2 |
| 72 | Azure Service Bus topology `LogResult` | 0/6 |
| 72 | RabbitMQ topology `LogResult` | 0/7 |
| 72 | Sagas `ScheduleActivity.ExecuteAsync` state machine | 0/10 |
| 67 | RabbitMQ `RabbitMqEndpointAddress` constructor | 98/99 |
| 66 | Job Service `CronExpression.StoreExpressionGeneralValue` | 66/66 |
| 62 | Job Service partition-key formatter registration | 33/33 |
| 62 | Job Service correlation conventions registration | 36/36 |
| 57.95 | Azure Service Bus receiver exception handler | 29/34 |
| 57.01 | Azure Service Bus subscription option projection | 14/24 |
| 54.65 | Azure Service Bus host endpoint parser | 28/37 |

The immediate coherent slice is Core scheduling: it contains the two highest uncovered Core
methods and connects directly to the uncovered Saga scheduling activity. The remaining A+ work is
then selected from the distribution above using behavior risk, branch gaps, and mutation-resistant
test opportunities. Fully covered methods with high cyclomatic complexity require production
decomposition rather than artificial coverage tests.

Global A+ remains open: line and branch coverage are below A+, and 83 methods remain above CRAP
30.

Independent read-only adversarial review reproduced the report hashes, assembly set, line and
branch totals, method identities, and CRAP arithmetic after the aggregator corrections. It returned
PASS with no remaining concrete finding.
