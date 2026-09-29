# T117 — Cron evaluation at representable time boundaries

## Frozen product and test bytes

Implementation/test commit: `f45b7d8460720314911b08d702ae2d09d3493ba5`.
Source tree: `7d8c56b28ac32227355ee9e4354da9771ef6e8a1`.
Test tree: `4ae845dbf5745dab981e32f5b0309c039ea32ed4`.

`GetTimeAfter(DateTimeOffset.MaxValue)` previously threw while adding one
second before checking the supported cron year range (1970–2199). A new
test failed red-first with `ArgumentOutOfRangeException` at the addition.
`IsSatisfiedBy(DateTimeOffset.MinValue)` also failed red-first with the same
exception while subtracting one second. The source now returns null after
UTC year 2200 and false at the earliest UTC instant, respectively. The
upper guard retains the following UTC year because a local appointment in
2199 can occur there.

The new product tests prove three distinct contracts:

- `MaximumInstant_ExhaustsTheSupportedYearWithoutOverflow`: both next-time
  APIs return null and `IsSatisfiedBy` is false at the maximum instant.
- `MinimumInstant_IsUnsatisfiedAndFindsTheFirstSupportedYear`: at the minimum
  UTC instant represented with offsets 0 and +14, satisfaction is false;
  both next-time APIs find the exact first scheduled 1970 instant, and that
  instant satisfies the expression.
- `LastSupportedLocalYear_RemainsVisibleAcrossTheUtcYearBoundary`: UTC-10 and
  UTC-14 zones schedule local 2199-12-31 23:59:59 in UTC year 2200. Both
  next-time APIs find the exact instant, `IsSatisfiedBy` confirms it, and
  the next search is exhausted. The UTC-14 row protects the extreme offset
  against an overly early year cutoff.

## Verification and limits

- Red-first: maximum case failed at `CronExpression.GetTimeAfter` line 1322;
  minimum case failed at `CronExpression.IsSatisfiedBy` line 113. These are
  direct old-product counterexamples, not speculative mutants.
- Final Release build: zero warnings and errors. Focused Cron test classes:
  237/237 passed, no failures or skips. Complete Core project: 7,116/7,116
  passed, no failures or skips, on the frozen implementation/test commit.
- Independent read-only adversarial review found two P2 gaps in the first
  version: UTC-10 did not prove the full negative-offset boundary, and three
  requirement attributes lacked JSON projection. Both were corrected.
  Final re-review: PASS, no remaining concrete P1/P2. `git diff --check` and
  JSON parsing pass.

The Microsoft `code-testing-agent` Research → Plan → Implement workflow was
used inline. The T98 Roslyn `find-untested-sources` pairing was reused as a
static pointer; `test-gap-analysis` and `assertion-quality` informed the
boundary and exact-outcome assertions; `run-tests` supplied the SDK 10/MTP
commands. T114 remains the latest full 33-profile Line/Branch/CRAP snapshot.
This packet adds a product fix and tests but makes no current global A+
claim; the full product measurement is due at the agreed grouped checkpoint.
