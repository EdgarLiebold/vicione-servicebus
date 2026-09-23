# Product-wide coverage profile at a087aaa64

## Scope and evidence

- Source and test commit: `a087aaa6482231878fe74e72be6d32305d62dce9`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- The 36 fresh Cobertura reports are under
  `artifacts/coverage-a-plus-20260923-a087aaa64/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one supplementary Abstractions run with
  `DOTNET_EnableAVX2=0`. All counted test commands exited successfully.
  The complete serial Unit/Architecture gate passed 10,203/10,203 tests
  on the worktree content subsequently committed as `a087aaa64`; its log
  predates the commit. Architecture tests are outside the instrumented
  aggregate because their assembly-ownership assertion is affected by
  instrumentation.
- All 32 product assemblies in the preceding complete profile occur in the
  reports. Report hashes, 77 product-binary hashes, exact HEAD and four
  fixture records are in the artifact-local `provenance.json`; the method
  inventory is in `analysis-36/methods.json`. Each fixture's
  `fixture-findings.json` has an empty findings list.
- Locked restores and Release builds used separate artifact roots for the
  supplementary Unit projects and provider projects. The provider solution
  and three individually built provider solutions completed with zero warnings
  and errors. The 18 other Unit modules used Release binaries
  from the preceding full Unit/Architecture gate on that content. Coverage used
  `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
- Binary and report hashes describe the collected files, but do not alone
  establish a report-to-binary or build-to-commit chain. The exact-HEAD claim
  also relies on the observed commands and empty tracked source/test diff.

## Product-wide result

| Measure | Current `a087aaa64` | Previous `8d6621c48` |
| --- | ---: | ---: |
| Line coverage | 83,636 / 93,404 = 89.5422% | 83,625 / 93,404 = 89.5304% |
| Branch observation, conservative merge | 29,946 / 36,737 = 81.5145% | 29,944 / 36,737 = 81.5091% |
| Branch observation, capped sum | 32,442 / 36,737 = 88.3088% | 32,433 / 36,737 = 88.2843% |
| Methods with CRAP > 30 | 67 / 25,920 | 68 / 25,920 |

`AssemblyTypeCache.ThrowIfAnyTypeScanFailures` now has 6/6 measured lines
and CRAP 6.00 in the global aggregate; the preceding complete profile measured
0/6 and CRAP 42. The hard behavior test and its adversarial review are
documented in `assembly-scan-failure-phase.md`. Other measured line changes
across reports are descriptive, since test execution and instrumentation can
vary.

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

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72, 0/1 covered lines.
  A synthetic reflection state would raise its measured coverage without
  proving a reachable public property contract; investigate real failure
  scenarios before adding a test.
- `RabbitMqEndpointAddress` constructor: CRAP 67 and 98/99 covered lines;
  complexity dominates its remaining risk.
- `CronExpression` and JobService registration methods: several methods are
  above CRAP 30 despite full measured line coverage; behavior-led
  simplification is needed before more tests could lower their scores.
- Azure Service Bus receiver exception handling and endpoint parsing:
  uncovered failure paths still carry high CRAP and need failure oracles.

The exact method ordering and source locations are in `analysis-36/methods.json`.
