# Product-wide coverage profile at 8d6621c48

## Scope and evidence

- Source and test commit: `8d6621c483c781669d226242afa5567372e7aca4`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- The 36 fresh Cobertura reports are under
  `artifacts/coverage-a-plus-20260923-8d6621c48/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one supplementary Abstractions run with
  `DOTNET_EnableAVX2=0`. All counted test commands exited successfully.
  The earlier complete serial Unit/Architecture gate at this commit passed
  10,202/10,202 tests. Architecture tests are outside the instrumented
  aggregate because their assembly-ownership assertion is affected by
  instrumentation.
- An initial supplementary-Unit build script stopped before running the third
  module because its project-file name was wrong (`MSB1009`). The corrected
  retry built all four modules and produced their successful test reports;
  both attempt logs remain in the artifact root.
- All 32 product assemblies in the preceding complete profile occur in the
  reports. Report hashes, 77 product-binary hashes, exact HEAD and four
  fixture records are in the artifact-local `provenance.json`; the method
  inventory is in `analysis-36/methods.json`. Each fixture's
  `fixture-findings.json` has an empty findings list.
- Locked restores and Release builds used separate artifact roots for the
  supplementary Unit projects and provider projects. The provider solution
  and three individually built provider solutions completed with zero warnings
  and errors. The 18 other Unit modules used the exact-HEAD Release binaries
  from the preceding full Unit/Architecture gate. Coverage used
  `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
- Binary and report hashes describe the collected files, but do not alone
  establish a report-to-binary or build-to-commit chain. The exact-HEAD claim
  also relies on the observed commands and empty tracked source/test diff.

## Product-wide result

| Measure | Current `8d6621c48` | Previous `09235f22d` |
| --- | ---: | ---: |
| Line coverage | 83,625 / 93,404 = 89.5304% | 83,609 / 93,404 = 89.5133% |
| Branch observation, conservative merge | 29,944 / 36,737 = 81.5091% | 29,930 / 36,737 = 81.4710% |
| Branch observation, capped sum | 32,433 / 36,737 = 88.2843% | 32,430 / 36,737 = 88.2761% |
| Methods with CRAP > 30 | 68 / 25,920 | 69 / 25,920 |

`DurableSendQuarantineEntry.Validate` now has 14/14 measured lines and CRAP
20.00 in the global aggregate; the preceding complete profile measured 8/14
and CRAP 51.49. This is the behavior-led regression slice documented in
`durable-quarantine-validation-phase.md`. Other measured line changes across
reports are descriptive, since test execution and instrumentation can vary.

Cobertura supplies per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. One branch location has
different valid counts across reports:
`MessagePackMessageSerializer.cs:194` has 2/2 in the MessagePack Unit report
and 0/4 in several provider reports. Consequently these observations are not
formal bounds on exact merged branch coverage. CRAP uses
`complexity² × (1 − method line coverage)³ + complexity`.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. Coverage and
CRAP remain below the requested product-wide A+ target.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72, 0/1 covered lines;
  inspect its compilation-failure behavior and test oracle.
- `RabbitMqEndpointAddress` constructor: CRAP 67 and 98/99 covered lines;
  complexity dominates its remaining risk.
- `CronExpression` and JobService registration methods: several methods are
  above CRAP 30 despite full measured line coverage; behavior-led
  simplification is needed before more tests could lower their scores.
- Azure Service Bus receiver exception handling and endpoint parsing:
  uncovered failure paths still carry high CRAP and need failure oracles.

The exact method ordering and source locations are in `analysis-36/methods.json`.
