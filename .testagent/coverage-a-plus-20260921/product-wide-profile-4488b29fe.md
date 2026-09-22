# Product-wide coverage profile at source/test commit 4488b29fe

## Scope and provenance

- Exact source/test commit: `4488b29fedb456defa8fdcd1f5984f8e165ee72a`.
  The tracked `src`/`tests` diff was empty throughout this measurement.
- Microsoft CodeCoverage used `tools/ci/coverage.settings.xml`. All 36 fresh,
  parseable Cobertura reports are under
  `artifacts/coverage-a-plus-20260922-4488b29fe/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one Abstractions run with `DOTNET_EnableAVX2=0`.
  `analysis-36/summary.json` records every report path and SHA-256; all 32
  loadable source assemblies were observed, with none missing or unexpected.
- The 22 Unit modules passed 9,552/9,552 tests. The 13 provider modules passed
  544/544; the supplementary Abstractions run passed 751/751. Every counted run
  had zero failures and skips. Six canonical fixture runs have empty findings:
  `vicione-debd01561735`, `vicione-08a561869d94`,
  `vicione-58b552c10043`, `vicione-89c10de91c17`,
  `vicione-3b61904a24cd`, and `vicione-4a4548210d84`.
- The direct no-fixture attempt could not run Azure Table because the local
  profile and endpoint credentials were absent. A later shared-fixture attempt
  passed its first six projects but ActiveMQ failed 2/100 tests because the
  outage control was not enabled. Reports from the no-fixture attempt,
  including the first successful Abstractions run, are in
  `invalid-no-fixture-attempt/`. The failed ActiveMQ report is separately in
  `invalid-missing-outage-control-attempt/`. Both directories are outside
  `raw/`. ActiveMQ then passed 100/100
  with the canonical `--allow-broker-outage activemq` fixture. The first six
  successful shared-fixture reports remain in `raw/` and have an empty fixture
  finding file. Neither failed ActiveMQ run nor no-fixture run contributes to
  the aggregate.
- Architecture tests remain outside this coverage aggregate because coverage
  instrumentation changes an assembly-ownership assertion. The last complete
  Unit/Architecture gate on this exact source/test commit passed 9,997/9,997;
  its evidence is in `eventhub-header-provider-phase.md`.

## Product-wide result

| Measure | Current result | Previous full profile at 44d9e3254 |
| --- | ---: | ---: |
| Line coverage | 83,067 / 93,200 = 89.1277% | 82,927 / 93,153 = 89.0224% |
| Branch coverage, conservative lower bound | 29,565 / 36,649 = 80.6707% | 29,470 / 36,629 = 80.4554% |
| Branch coverage, capped upper bound | 32,111 / 36,649 = 87.6177% | 32,018 / 36,629 = 87.4116% |
| Methods with CRAP > 30 | 97 / 25,886 | 105 / 25,880 |

Cobertura reports per-line branch counts but not branch identities. The
aggregate merges unique source class/line counts across suites. The lower
bound takes the maximum observed covered count; the upper bound caps the sum.
Neither is an exact global branch rate. The explicit coverage settings and
merging method match the previous complete profile, so the small before/after
change is informative, but source and test additions also changed the
denominators.

## Highest current CRAP risks

| CRAP | Method | Covered source lines |
| ---: | --- | ---: |
| 156 | PostgreSQL `MaintenanceAgent.PerformMaintenanceAsync` closure | 0/8 |
| 156 | Abstractions `DictionaryExtensions.SetValue` | 0/11 |
| 124.37 | Amazon SQS receive-endpoint `Validate` iterator | 24/36 |
| 110 | Abstractions `RequestRateAlgorithm.ChangeRateLimitAsync` | 0/17 |
| 110 | Core `TimeoutActivityContextProxy.NotifyFaultedAsync` | 0/11 |
| 110 | Core recurring scheduler converter | 0/12 |
| 104 | Job Service `JobStateMachine` constructor | 167/167 |
| 96 | RabbitMQ `ConnectionContextFactory.CreateConnectionAsync` | 19/38 |
| 94.98 | RabbitMQ send transport `SendAsync` | 79/85 |
| 82.54 | Core `CircuitBreakerSettings.Validate` iterator | 8/15 |

The PostgreSQL maintenance closure runs real database cleanup only after its
timer fires; the existing PostgreSQL provider tests do not exercise that
window. The public request-rate change operation has no direct behavior tests
for increasing, decreasing, canceling, or rejecting changes. These are
candidate behavior slices, subject to direct source/test review before editing.
The fully covered Job Service constructor remains above CRAP 30 because its
reported cyclomatic complexity is 104; tests alone cannot lower that score.

The most uncovered product lines are Core (3,883), Abstractions (1,134), Azure
Service Bus (863), and RabbitMQ (713). Global A+ remains open.

An independent read-only adversarial review reproduced the line, branch, and
CRAP totals, checked all 36 report paths and hashes, all six fixture findings
and broker-log hashes, and the passing test summaries. It found one evidence
directory naming error, corrected by separating the two rejected attempts;
the final review returned PASS with no remaining concrete findings.
