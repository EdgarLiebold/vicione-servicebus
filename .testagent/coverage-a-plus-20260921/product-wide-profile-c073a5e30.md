# Product-wide coverage profile at c073a5e30

## Scope and evidence

- Exact product/test HEAD: `c073a5e30f5d50ebf8fa062ce056438f562f6030`.
  The tracked `src`/`tests` diff was empty during aggregation. User-owned
  untracked `TestResults/` and `review/` were excluded.
- All 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-c073a5e30/raw/` parse and observe all
  32 product assemblies: 18 native Unit, four additionally instrumented Unit,
  13 local-provider, and one supplementary Abstractions run without AVX2.
- The five collection logs show 11,123 passing executions, zero failures and
  skips: 9,517 native Unit, 280 additional Unit, 774 no-AVX2 repeat, 417
  broad local-provider, and 135 separately brokered local-provider executions.
- The four successful fixtures are `vicione-af18fe5af7d6` (six brokers and
  controlled ActiveMQ outage), `vicione-4aa768ab5006` (SQL Server),
  `vicione-5d3a250930c7` (RabbitMQ), and `vicione-de34732e4bbe` (Azure
  Service Bus). Their `fixture-findings.json` lists are empty. The local-provider
  graph built with zero warnings and errors. The preceding complete Release
  Unit/Architecture gate at this source/test commit passed 10,242/10,242.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record the
  aggregation, exact HEAD, report hashes, collection scripts, 77 product
  binaries, fixture records, and broker-log hashes. SHA-256 values are
  `3f5748f3e79095c72836d092676e6439bf7aca24e4af4b5cd873669fd5146cde`
  for summary, `d107001242a14247ca24fdb386a63ebf5abe1527230306948b75085f5a7771b6`
  for methods, and `856508c13d026547d25d6905e554a693b74a37de1b738a5ae60ddf8d28f3d3e1`
  for provenance.
- The no-AVX2 command first wrote its report to a new subdirectory under
  `TestResults/` because its output argument was relative. I checked that the
  subdirectory contained only that generated file, moved it to this profile's
  `raw/no-avx2/` directory, and removed only the empty new subdirectory. The
  successful test log still names the original location; the report bytes are
  preserved in the profiled raw directory and their hash is in provenance.
- Independent read-only adversarial review reaggregated all 36 raw XML files,
  verified the logs, hashes, fixture findings, HEAD, and `src`/`tests` diff,
  and returned PASS. It retained the documented no-AVX2 attribution limit.

## Product-wide result

| Measure | Current `c073a5e30` | Previous `c0cab32ba` |
| --- | ---: | ---: |
| Line coverage | 83,795 / 93,439 = 89.6788% | 83,751 / 93,419 = 89.6509% |
| Branch observation, conservative merge | 30,015 / 36,672 = 81.8472% | 29,989 / 36,672 = 81.7763% |
| Branch observation, capped sum | 32,487 / 36,672 = 88.5880% | 32,463 / 36,672 = 88.5226% |
| Methods with CRAP > 30 | 56 / 25,953 | 59 / 25,943 |

The two JobService registration entry points moved from CRAP 62 to CRAP 2
and 1; their largest new private helpers score 22 and 20. `NewId.op_Equality`
moved from CRAP 42 to 6. The boxed NewId equality and comparison paths are
also fully covered and score CRAP 4 each. These reductions correspond to
behavioral tests and a registration-preserving split reviewed read-only by
the adversarial reviewer. The difference in aggregate line counts also
includes new source lines and run variability; it is not assigned solely to
the selected changes.

Cobertura supplies per-line branch counts without stable branch identities.
The conservative observation takes the largest hit count at each branch
location; the capped sum adds observations up to the largest valid count.
Neither value is a formal lower or upper bound on true combined branch
coverage. CRAP is `complexity² × (1 − method line coverage)³ + complexity`,
following the Microsoft `coverage-analysis` skill. The no-AVX2 condition is
recorded by its invocation, not independently encoded in its XML. Hashes
preserve file identity but do not alone prove execution provenance; the
ordered commands and unchanged tracked source/test files provide that link.

## Remaining A+ work

Global A+ remains open for line and branch coverage and for 56 methods above
CRAP 30. The leading methods are `PropertyAccessorFactory.IsCompilationFailure`
(72, unexecuted), `CronExpression.StoreExpressionGeneralValue` (66, fully
covered but complex), Azure Service Bus receiver exception handling (57.95),
and `CronExpression.ProgressNextFireTimeDayOfWeek` (53.38). The dual-saga SQL
partition-key ordering contract and NewId high-bit ordering direction also
remain explicit behavioral review points. No whole-product A+ claim is made.
