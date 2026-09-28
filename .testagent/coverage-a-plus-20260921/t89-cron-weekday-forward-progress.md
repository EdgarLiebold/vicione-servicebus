# T89 — nearest-weekday cron progression

Test/product commit: `b918ab74e`.

## Product defect and correction

For `15W`, `30W`, and `31W`, a date adjusted backward from a weekend
could be returned again by `GetTimeAfter` after it had already fired. The
date-set helper allowed a candidate before the search cursor. Removing that
exception alone exposed the second half of the defect: entering a new month
at the nominal day skipped its earlier adjusted weekday. `CronExpression`
now enters an exhausted next month at day 1 and recalculates its eligible
date. The helper only selects days at or after the current cursor. The
unused negative-offset state was removed.

## Evidence

- Baseline counterprobe: the new fixed-month theory failed in 3 of 32
  scheduling cases because `GetTimeAfter(expected)` returned `expected`
  again. After the first partial fix, existing `15W` contract test failed:
  expected 2024-06-14, got 2024-07-15. Both failures are closed by the
  final correction.
- Focused scheduling suite: 36/36 passed, no failures or skips.
- Complete Core project on exact commit `b918ab74e`: 6,956/6,956 passed,
  no failures or skips.
- `NearestWeekday_StaysWithinItsMonthAndFiresExactlyOnce` verifies five
  boundary calendars, an exclusive next occurrence, membership and adjacent
  non-membership. `NearestWeekday_AfterFiringMovesToTheAdjustedDayInTheNextMonth`
  verifies three successive occurrences across four month transitions,
  including leap February and backward weekend adjustments.
- Independent read-only Red Team first identified the skipped-month P1 in
  the partial fix, then re-reviewed the final code and test: PASS, no P1/P2.
- Both new requirement variants are projected in `CoreRequirements.json`.

## Measurement boundary

T85 is still the latest complete 33-profile Line/Branch/CRAP measurement.
This packet improves behavior and focused verification; it does not claim a
new product-wide A+ result. The next full profile follows the agreed larger
packet cadence.
