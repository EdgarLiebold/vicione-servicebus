# Product-wide coverage profile at 09235f22d

## Scope and evidence

- Source and test commit: `09235f22d07ae9963b22c43fa99e0d3c5446bee8`.
  The tracked `src`/`tests` diff was empty during aggregation, with SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- All 36 fresh Cobertura reports are under
  `artifacts/coverage-a-plus-20260923-09235f22d/raw/`: 22 Unit/Infrastructure,
  13 local-provider, and one supplementary Abstractions run with
  `DOTNET_EnableAVX2=0`. All counted test commands exited successfully.
  Architecture tests are outside this instrumented aggregate because their
  assembly-ownership assertion is affected by instrumentation.
- All 32 product assemblies in the previous complete profile occur in the new
  reports. The report hashes, product-binary hashes, exact HEAD and four fixture
  records are in the artifact-local `provenance.json`; the method inventory is
  in `analysis-36/methods.json`. Each fixture's `fixture-findings.json` has an
  empty findings list.
- Locked restores and Release builds used separate artifact roots for the
  supplementary Unit projects and the provider projects. The provider solution
  and three individually built provider solutions completed with zero warnings
  and errors. The 18 other Unit modules used the exact-HEAD Release binaries
  from the prior full Unit/Architecture gate. The four supplementary Unit
  modules were built at this HEAD. Coverage used
  `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
- Binary and report hashes describe the collected files, but do not alone
  establish a report-to-binary or build-to-commit chain. The exact-HEAD claim
  also relies on the observed commands and the empty tracked source/test diff.

## Product-wide result

| Measure | Current `09235f22d` | Previous `ac363722c` |
| --- | ---: | ---: |
| Line coverage | 83,609 / 93,404 = 89.5133% | 83,389 / 93,236 = 89.4386% |
| Branch observation, conservative merge | 29,930 / 36,737 = 81.4710% | 29,798 / 36,715 = 81.1603% |
| Branch observation, capped sum | 32,430 / 36,737 = 88.2761% | 32,315 / 36,715 = 88.0158% |
| Methods with CRAP > 30 | 69 / 25,920 | 83 / 25,917 |

Cobertura supplies per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. One of 14,717 branch
locations has different valid counts across reports, so these observations
are not formal bounds on exact merged branch coverage. The location is
`MessagePackMessageSerializer.cs:194`: the MessagePack Unit report has 2/2
branches and several provider reports have 0/4. CRAP uses
`complexity² × (1 − method line coverage)³ +
complexity`. The current source and test changes also alter denominators, so
the profile-to-profile difference is descriptive.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. Coverage and
CRAP are still below the requested product-wide A+ target.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72, 0/1 covered lines;
  inspect whether the compilation-failure path needs a strong regression test.
- `RabbitMqEndpointAddress` constructor: CRAP 67 and 98/99 covered lines;
  complexity itself is the dominant remaining risk.
- `CronExpression` and JobService registration methods: several methods remain
  above CRAP 30 even with all measured lines covered; these need behavior-led
  simplification before extra tests could improve the score.
- Azure Service Bus receiver exception handling and endpoint parsing:
  uncovered error paths still carry high CRAP and need failure oracles.

The exact method ordering and source locations are in `analysis-36/methods.json`.
