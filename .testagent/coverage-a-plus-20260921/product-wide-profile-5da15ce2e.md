# Product-wide coverage profile at 5da15ce2e

## Scope and evidence

- Source and test commit: `5da15ce2e65690e81533f12ec64833b5d9534560`.
  The tracked `src`/`tests` diff was empty at aggregation, SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- All 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-5da15ce2e/raw/` came from test commands
  executed after that commit: 22 Unit/Infrastructure, 13 local-provider, and
  one Abstractions portability run with `DOTNET_EnableAVX2=0`. Interactive
  command output showed every counted test run passing without failures or
  skips, the uninstrumented Unit/Architecture gate passing 10,219/10,219,
  and the complete Release build finishing with zero warnings/errors. These
  outcomes were observed during execution, but this profile does not archive
  the test/build console logs and therefore cannot independently attest them.
  Architecture is excluded from the instrumented aggregate because
  instrumentation changes its assembly-owner assertion.
- Reports cover all 32 product assemblies. `analysis-36/summary.json` contains
  the aggregate and per-report hashes; `analysis-36/methods.json` contains
  method data. `provenance.json` records the exact HEAD, 36 report hashes, 77
  product-binary hashes, and four fixture records. The broad fixture used the
  required ActiveMQ outage control; SQL Server, RabbitMQ, and Azure Service Bus
  had separate fixtures. All four fixture findings lists are empty.
- One preliminary Solution test command selected zero tests because of an
  incompatible combination of extra CLI options; the subsequent plain
  `dotnet test --solution ... --no-build` ran the full green gate. A preliminary
  broad-provider fixture correctly stopped at zero discovered tests because
  its restore had received a relative MSBuild coverage-overlay path. A locked
  restore and warning-free build with the absolute overlay path produced
  CodeCoverage-enabled test hosts; the fresh replacement fixture passed all
  ten modules. No zero-test or incomplete coverage report is counted in `raw/`.
- Coverage used `tools/ci/coverage.settings.xml` and Microsoft CodeCoverage.
  Hashes identify the files collected after the run but alone do not prove
  the executed-binary-to-report or build-to-commit chain. Exact-commit
  attribution also relies on the observed build/test sequence and unchanged
  tracked source/test content. A final A+ evidence run must archive its
  execution logs alongside the reports and hash them in its provenance.

## Product-wide result

| Measure | Current `5da15ce2e` | Previous `175017360` |
| --- | ---: | ---: |
| Line coverage | 83,684 / 93,417 = 89.5811% | 83,660 / 93,417 = 89.5554% |
| Branch observation, conservative merge | 29,926 / 36,684 = 81.5778% | 29,919 / 36,684 = 81.5587% |
| Branch observation, capped sum | 32,409 / 36,684 = 88.3464% | 32,402 / 36,684 = 88.3273% |
| Methods with CRAP > 30 | 65 / 25,943 | 66 / 25,943 |

`DelegatePropertyProvider.GetPropertyAsync` improved from 0/8 lines and CRAP
42.00 to 5/8 and CRAP 7.90. The one-method reduction in the CRAP>30 count
comes from that actual consume-pipeline contract test. The product-wide A+
target remains open.

Cobertura gives per-line branch counts without branch identities. The
aggregator merges unique source lines across reports. Its conservative branch
merge takes the largest observed covered count per location; its capped sum
adds observed counts up to the largest valid count. Reports still disagree on
the valid count at `MessagePackMessageSerializer.cs:194`, so the two branch
observations are not formal bounds on exact merged branch coverage. CRAP uses
`complexity² × (1 − method line coverage)³ + complexity`.

The Microsoft `coverage-analysis` skill guided collection and risk review.
Its PowerShell scripts could not run because `pwsh` is unavailable here; the
artifact-local Python aggregator applies the documented formula. A+ remains
unreached in line coverage, branch coverage, and method CRAP.

## Next code areas

- `PropertyAccessorFactory.IsCompilationFailure`: CRAP 72 with 0/1 measured
  lines. The public Reflection fallback behavior already has strong tests;
  do not force a compiler-failure catch merely to change a score.
- `CronExpression.StoreExpressionGeneralValue` and JobService partition-key
  and correlation registration are fully line-covered yet have CRAP 66/62/62.
  Review behavior and simplify only where the same contracts remain protected.
- Azure Service Bus `Receiver.ExceptionHandlerAsync` has 29/34 lines and CRAP
  57.95; Amazon SQS subscription-attribute comparison has 6/11 and CRAP 48.43.
  Check reachable provider failure paths before adding tests.

The complete risk ordering is in `analysis-36/methods.json`.
