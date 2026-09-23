# Cron named-field parser complexity

## Change and behavioral evidence

The preceding 36-report product profile at exact commit `c073a5e30` recorded
`CronExpression.StoreExpressionGeneralValue` at CRAP 66 with full line
coverage. This is a structure hotspot: more tests alone cannot reduce its
complexity floor. The parser now dispatches named month and weekday fields to
focused methods. The existing suffix, named range, increment, ordinal, and
last-weekday paths retain their values and error messages. No comments were
changed by script.

`CronExpressionCalendarTests.NamedMonthRange_WrapsAcrossDecemberAndJanuaryWithoutSkippingAYear`
adds a product-level regression for `DEC-JAN`. It asserts the next actual UTC
fire time at December 1, January 1, and the following December 1. Thus it
would detect a lost wraparound month, an inclusive rather than exclusive next
fire time, or an incorrect year advance. The existing 214 Cron tests also
exercise named month and weekday parsing, exact failures, and weekday
calendars. The new test has no requirement annotation because the immutable
`CoreRequirements.json` projection was not extended in this phase.

## Verification

- Current-byte Cron suite: 215/215 passed, zero failures and skips.
- Current-byte complete Core suite: 6,379/6,379 passed, zero failures and
  skips. This includes the requirement projection test.
- Microsoft CodeCoverage Cobertura from the 215-test Cron suite:
  `artifacts/coverage-cron-named-fields-20260923/core-focused.cobertura.xml`,
  SHA-256 `e8af475c2ec588681f71b031294f0a30004195f1493568ea2890baa1a1e31267`.
- Independent read-only adversarial diff review returned PASS after checking
  named suffixes, span indices, exact error messages, and projection metadata.

| Method | Covered lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| `StoreExpressionGeneralValue` | 13/14 | 10 | 10.04 |
| `ParseNamedMonth` | 25/25 | 16 | 16 |
| `ParseNamedDayOfWeek` | 20/20 | 16 | 16 |
| `ParseNamedWeekCount` | 5/5 | 8 | 8 |
| `ParseNamedDayOfWeekRange` | 14/14 | 8 | 8 |

These scores are limited to the named-field methods and the current-byte
focused coverage report. A new exact-commit, product-wide 36-report profile
is still required before updating global line, branch, or CRAP counts; global
A+ remains open.

## Open behavior question

The read-only Red Team found that `FRI/2` and numeric `6/2` currently produce
different schedules. In June 2007, the existing `FRI/2` test expects the 1st,
15th, and 29th, while `6/2` resolves to every Friday. This phase preserves
both existing behaviors. A separate product contract decision is needed before
changing either syntax, because the former behavior is already tested.
