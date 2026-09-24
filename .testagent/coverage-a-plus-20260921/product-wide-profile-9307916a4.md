# Cumulative product coverage profile at 9307916a4

## Scope and validation

- Product/test HEAD: `9307916a464495ba000a1bc06a38343cc0e46d0e`.
  The tracked `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
  Since `db74e2489`, the sole changed product source is
  `src/ViciOne.ServiceBus.JobService/Configuration/JobSagaDefinition.cs`.
- The complete Release Unit/Architecture build passed with zero warnings and
  errors. The two focused JobService receive partition tests passed 2/2.
  The complete Core run passed 6,386/6,386 under Microsoft CodeCoverage at
  this HEAD and again without coverage in an isolated run.
- During concurrent full-solution and Core coverage runs, six Core tests
  failed with Reflection `InvalidCastException`; the successful isolated Core
  runs did not reproduce them. The concurrent gate was stopped after the
  failure. The subsequent serial full Unit/Architecture gate passed
  10,342/10,342, with zero failures and zero skipped tests (exit 0).
- `artifacts/coverage-a-plus-20260924-9307916a4/raw/` contains 38 parseable
  Cobertura reports for 32 product assemblies. The 37 prior reports are
  individually hash-identical to the `db74e2489` profile. The added Core
  report was generated at this HEAD. These are 36 project slots with three
  Core observations, not 38 executions at this HEAD. All 12 broker fixture
  records are inherited; no new broker run occurred in this iteration.

## Product-wide cumulative result

| Measure | `9307916a4` | Previous `db74e2489` |
| --- | ---: | ---: |
| Line coverage | 83,987 / 93,480 = 89.8449% | 83,978 / 93,472 = 89.8429% |
| Conservative branch observation | 30,112 / 36,660 = 82.1386% | 30,111 / 36,660 = 82.1358% |
| Methods with CRAP > 30, exact arithmetic | 36 / 25,989 | 37 / 25,985 |

`JobSagaDefinition.ConfigureSaga` drops from complexity/CRAP 40 with 29/29
covered lines to complexity/CRAP 4 with 15/15. Its four extracted partition
groups have complexity/CRAP 8, 10, 10, and 8, each fully covered in the merged
profile. The new registered-saga test checks the exact set of 18 coordination
message types, one partition filter per type, a single coordinator identity,
partition count two, and endpoint concurrency limit two. The separate existing
direct-endpoint test verifies same-job serialization across two message types
while another partition continues. The refactor preserves all 18 selectors
and their order by source diff; the tests do not claim to observe that order.

## Overlay method and limits

- The raw reports are retained unchanged. In the analyzer, classes whose
  normalized source is the changed `JobSagaDefinition.cs` are excluded from
  each of 11 older reports that contain that file. Only the fresh, SHA-bound
  Core report supplies current classes from it. The analyzer requires the
  constructor, `ConfigureSaga`, and all four partition helpers in the fresh
  report, plus exactly 11 exclusions. The other product source files are
  unchanged and keep their earlier coverage observations.
- `analysis-38/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record the merged counts, excluded reports, exact
  method threshold decisions, source and binary origins, and fixture chain.
  Their SHA-256 values are respectively
  `f7263160c155887851edd11eb219f26795711cf0f414208d64258808395ff432`,
  `bea3401d74389ec2597f0f2ba680ea761e08d6a3c7d6298a995d086f7895e857`,
  `02b7e895562a27d7fd92645cd11ea1207128137a074a2ab060a5b40e1f184d3a`,
  and `e9d0fc1e6cb0c0bc3a02b37910614f46e2868edb5bd89662cf59baf96408838f`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. The capped sum is
  inflated by repeated Core observations and is not used for comparison.
  CRAP is `complexity² × (1 − method line coverage)³ + complexity`, with
  the `> 30` threshold checked using exact integer arithmetic.
- Successful command exits were observed but execution logs were not
  persisted. Existing user-owned untracked `TestResults/` and `review/` were
  left intact. The next largest uncovered risks are the three public
  `ConsumeObserverConverter<T>` callbacks and several provider methods at
  CRAP 42; functional use must guide the next tests.

The A+ line, branch, and CRAP objective remains open. The full ranking is in
`analysis-38/methods.json`.
