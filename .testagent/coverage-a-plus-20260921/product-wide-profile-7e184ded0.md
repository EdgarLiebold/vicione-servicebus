# Product-wide coverage profile at 7e184ded0

## Scope and evidence

- Product/test HEAD: `7e184ded02438494c9deeddd96cc1fbefe31ae9f`.
  The tracked `src`/`tests` diff was empty at aggregation (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- All 36 new Cobertura reports under
  `artifacts/coverage-a-plus-20260923-7e184ded0/raw/` comprise 22
  Unit/Infrastructure runs, 13 local-provider runs, and one Abstractions
  portability run with `DOTNET_EnableAVX2=0`. The 36 counted commands
  passed 11,101 test executions with zero failures and skips; the portability
  run repeats 768 Abstractions tests. Reports cover all 32 product assemblies.
- `analysis-36/summary.json` and `methods.json` hold the merged measures and
  method data. `provenance.json` records the exact HEAD and SHA-256 hashes of
  36 reports, 14 log files, two analysis files, nine collection scripts, 77
  product binaries, and four successful fixture records. Their findings are
  empty; all nine referenced broker-log hashes match. The six-broker fixture
  included the required ActiveMQ outage control. SQL Server, RabbitMQ, and
  Azure Service Bus each ran in a separate fixture.
- The full Release build and instrumented provider builds completed with zero
  warnings/errors. The uninstrumented Unit/Architecture gate initially failed
  because a Quartz suspect-retry test timed out waiting 30 seconds for a
  status-check schedule while the broker fixture also ran. That test passed
  1/1 in isolation; the complete gate passed **10,226/10,226** on retry
  without parallel ServiceBus broker fixtures. The failed run and both
  successful retries are retained and hashed. The timeout's exact cause is
  unresolved. The test's natural status-check interval equals its 30-second
  wait budget, making a timing collision plausible; the failure did not reach
  the retry or stale-completion product path. The green retry does not erase
  the failed parallel run.
- A first provider restore exited 1 without diagnostics under restricted
  network access and produced no assets. The next locked restore with
  permitted network access succeeded. A RabbitMQ container then failed to
  start because it could not read its Erlang cookie (`eacces`); no test ran
  against that fixture. A fresh isolated RabbitMQ fixture passed 38/38, and
  only its report is counted. The failed attempts remain in the archived logs.

## Product-wide result

| Measure | Current `7e184ded0` | Previous `6a3689d33` |
| --- | ---: | ---: |
| Line coverage | 83,702 / 93,411 = 89.6061% | 83,702 / 93,411 = 89.6061% |
| Branch observation, conservative merge | 29,947 / 36,668 = 81.6707% | 29,939 / 36,668 = 81.6488% |
| Branch observation, capped sum | 32,426 / 36,668 = 88.4313% | 32,419 / 36,668 = 88.4122% |
| Methods with CRAP > 30 | 63 / 25,943 | 64 / 25,943 |

The RabbitMQ `RabbitMqDurableSendDispatcher.ValidateDestination` method now
measures 17/17 lines, 19/22 reported branches, complexity 22, and CRAP 22;
the previous full profile had 11/17 lines and CRAP 43.28. This is the sole
method that crossed below CRAP 30 in this iteration. The seven real-broker
destination cases and their adversarial review are documented in
`rabbitmq-durable-destination-phase.md`.

Cobertura supplies per-line branch counts without stable branch identities.
The aggregator merges unique source lines across reports, taking the largest
covered branch count per location for the conservative observation and adding
counts up to the largest valid count for the capped observation. Reports can
disagree about valid branch counts; these observations are not formal bounds
on exact combined branch coverage. CRAP is calculated as
`complexity² × (1 − method line coverage)³ + complexity`. The Microsoft
`coverage-analysis` skill guided collection and risk review. Its PowerShell
scripts could not run because `pwsh` is unavailable; the artifact-local Python
analyzer applies the documented formula. Hashes preserve artifacts but do not
alone prove the executed-binary-to-report or build-to-commit chain; that
attribution also relies on the observed ordered commands and unchanged
tracked source/test files. The console log and XML do not independently prove
the `DOTNET_EnableAVX2=0` environment setting of the portability command.

The product-wide A+ target remains open for lines, branches, and CRAP. Before
the next source/test change, inspect the Quartz timing failure under controlled
load and stabilize the manual-status-check test without weakening its behavior
assertions. The largest absolute unhit-line areas in this profile are Core,
Abstractions, and Azure Service Bus; select the next bounded product contract
from their actual behavior gaps, not the uncovered count alone.

An independent read-only adversarial review reaggregated the raw XML and
verified all report, log, analysis, script, binary, fixture, and broker-log
hashes plus the complete green gate retry. It returned PASS for this profile
under the limitations above.
