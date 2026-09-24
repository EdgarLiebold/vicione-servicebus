# Cumulative product coverage profile at db74e2489

## Scope and test evidence

- Product/test HEAD: `db74e24894f3dd6fe8df7beca41cd58f67f1c7af`.
  The tracked `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
  Since `162dab6d6`, product source is byte-identical; two Core scheduling
  tests and their requirement projection were added.
- The focused recurring-scheduler class passed 16/16 tests. The complete Core
  project passed 6,385/6,385 under Microsoft CodeCoverage at this HEAD;
  a repeated Core coverage run also passed 6,385/6,385. The complete
  Unit/Architecture gate passed 10,341/10,341 with no skips.
- `artifacts/coverage-a-plus-20260924-db74e2489/raw/` contains 37 parseable
  Cobertura reports for 32 product assemblies: all 36 reports from the
  `162dab6d6` profile are hash-identical and one additional Core Unit run was
  generated at this HEAD. Thus the profile still covers 36 project slots;
  Core has two observations. All 12 broker fixture records are inherited
  through the prior profile, with no new broker run in this iteration.
- `analysis-37/summary.json`, `methods.json`, and `provenance.json` contain
  merged counts, exact CRAP decisions, report/binary hashes, and the origin
  chain. Their SHA-256 values are respectively
  `fed2b66c7f561c831a8c599dc40f765f891827921d525b23f4156d314418bc61`,
  `326060d5dc8debd92385840d14e55aa5c242651e5441aa6c0d30a59dcdd5a9ed`,
  and `834e3beecf516123e985137c949924a15e07297c1c8387362fdc7d3d8b0403ae`.
  The fresh Core report SHA-256 is
  `3adc74af9490a992991ff03d8ed0ce028f8d817088c99153c9809807afc946d8`.

## Product-wide cumulative result

| Measure | `db74e2489` | Previous `162dab6d6` |
| --- | ---: | ---: |
| Line coverage | 83,978 / 93,472 = 89.8429% | 83,954 / 93,472 = 89.8173% |
| Conservative branch observation | 30,111 / 36,660 = 82.1358% | 30,104 / 36,660 = 82.1167% |
| Methods with CRAP > 30, exact arithmetic | 37 / 25,985 | 38 / 25,985 |

The public `PublishRecurringMessageScheduler.ScheduleRecurringSendAsync` overload
that takes an object, declared `Type`, pipe, and cancellation token improves
from 2/10 lines and CRAP 40.768 to 6/10 lines and CRAP 12.096. The new tests
check the published command's declared contract, schedule, payload, destination,
URN, pipe and token identity, and the returned typed handle. A failed publish
must propagate the same exception and cannot return a handle. The tests use
the public scheduler interface and inspect the publish boundary.

## Measurement caveat and next areas

- Replacing the old Core report with the fresh one would be an invalid
  comparison. Both runs pass all Core tests, and product source is unchanged,
  but the collector scope shifts: old Core XML has 9 packages and 4,843
  classes; fresh Core XML has 11 packages and 4,989 classes. Some unchanged
  generated async methods, including `InstanceMessageFilter.MoveNext`, are
  absent from the fresh report. A second fresh run reproduced that omission.
  Retaining the previous observation and adding the fresh one preserves valid
  coverage of the same product source while recording the new tests.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location, so it is a lower
  bound. The analyzer also computes a capped sum of 33,639/36,660, but the
  second Core observation can count the same branch twice. This value is not
  comparable with the previous profile and is not used as A+ evidence.
- CRAP is `complexity² × (1 − method line coverage)³ + complexity`, with the
  `> 30` threshold checked using exact integer arithmetic. The profile is a
  cumulative source-coverage view across runs, not 37 executions at this HEAD.
  Test command exits were observed, but execution logs were not persisted.
- Existing user-owned untracked `TestResults/` and `review/` were left intact.
  The next source areas should be selected for demonstrable product behavior;
  the three `ConsumeObserverConverter<T>` callbacks remain CRAP 42 with 0/9
  lines, but have no in-repository runtime call sites.

The A+ line, branch, and CRAP objective remains open. The full method ranking
is in `analysis-37/methods.json`.
