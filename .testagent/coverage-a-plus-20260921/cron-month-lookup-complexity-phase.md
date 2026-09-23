# Cron month-name lookup complexity

The preceding exact product profile at `c073a5e30` measured
`CronExpression.GetMonthNumber` at CRAP 48 despite full coverage. Its switch
expression encoded twelve fixed abbreviations and inflated the method's
reported complexity. The lookup now scans an ordered static list with exact
ordinal span equality. It returns the same zero-based index for every
supported abbreviation and `-1` for every other value. Scheduling itself
does not call this parser method.

No new test was added: the existing `MonthAbbreviation_MapsToTheExpectedCalendarField`
theory checks all twelve results, `InvalidNamedField_IsRejectedWithItsExactReason`
checks rejection, and the previous phase's `NamedMonthRange_WrapsAcrossDecemberAndJanuaryWithoutSkippingAYear`
checks actual fire times. A test of the private loop structure would not
assert a new product behavior.

- Current-byte Cron suite: 215/215 passed.
- Current-byte complete Core suite: 6,379/6,379 passed, zero failures/skips.
- Microsoft CodeCoverage report:
  `artifacts/coverage-cron-named-fields-20260923/month-lookup.cobertura.xml`,
  SHA-256 `caa67eeda29ed9da552b54ff212e58f7ebf9382884f1522190509cab6531d687`.
- `GetMonthNumber`: 8/8 lines covered, complexity 4, CRAP 4.
- Independent read-only adversarial review returned PASS for equivalent
  accepted/rejected values and parallel reads. The private array has no
  mutation site in the product code.

The complete product-wide profile still describes the earlier exact commit.
Global A+ remains open until a new 36-report profile and the other hotspots
are resolved.
