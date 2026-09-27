# T41: Cron parser and calendar boundaries

Base: `2512c46074cf53f8f48a3d1da1f389ff2ae638da`.
Microsoft code-testing-agent focused workflow and run-tests, MTP/xUnit v3.
CronExpression was read completely before editing. Call-site inspection and
independent read-only review establish that field tokens reach the private parser
normalized and nonempty, at offset zero. Remove its unused offset, unreachable
guards and unused whitespace helper. Simplify calendar selection after the
both-restart case, preserving weekday priority and global year exhaustion.
This is a semantics-preserving simplification, not a demonstrated product fix.

## Requirements and evidence

Tests are in `CronExpressionBoundaryRegressionTests`.

| Product contract | Test | Cases |
| --- | --- | --- |
| Invalid leading tokens in fields and lists are rejected by constructor, ValidateExpression and IsValidExpression | InvalidTokenStart_IsRejectedByEveryValidationEntryPoint | 2 |
| Unicode whitespace normalizes canonical text and preserves exact subsequent UTC occurrences | UnicodeWhitespace_PreservesCanonicalTextAndNextOccurrence | 3 |
| Day-of-month/day-of-week union crosses month boundaries, emits a coincident date once, and stops after the explicit year | CombinedCalendar_EnumeratesUnionWithoutSkippingOrDuplicatingDates | 1 |

Expected dates are literal calendar values, independently checked. These cases
do not establish all timezone/DST behavior or every supported cron expression.
No reflection, product test hooks or coverage exclusions were added.

## Verification

- MAIN `artifacts/t41-baseline.log`: 6/6 before product changes, exit 0.
- MAIN `artifacts/t41-scheduling.log`: 229/229, exit 0, no skips.
- MAIN `artifacts/t41-core.log`: 6731/6731, exit 0, no skips.
- MAIN `artifacts/t41-format.log`: verify-only formatting, exit 0.
- `artifacts/t41-cron.log` records an unsuccessful filter selection (zero tests,
  exit 5); the subsequent explicit scheduling namespace supplies the valid run.
- GATE `/private/tmp/servicebus-reply-investigation` deliberate mutations:
  - `artifacts/t41-mutant-token.log`: ignored invalid token, 2 failures/4 controls.
  - `artifacts/t41-mutant-whitespace.log`: narrowed normalization, 3 failures/3 controls.
  - `artifacts/t41-mutant-order.log`: reversed restart-date comparison, 1 failure/5
    controls; June 8 was skipped and June 15 returned.
- All three mutations exit 2 and were manually restored. Product, new test and
  requirements manifest compare byte-identically between MAIN and GATE.
- Read-only adversarial plan, implementation and evidence review found no concrete
  blocker. Restored GATE `artifacts/t41-restored.log`: 6731/6731, exit 0,
  no skips. Exact-commit product-wide measurement remains pending.

## Measurement requirements

The full 33-profile measurement must explicitly reconcile the changed private
StoreExpressionValues signature and removed SkipWhiteSpace identity. Cron line
numbers moved: compare unchanged source lines through a source mapping, not raw
line numbers. Distinguish deleted unreachable code from newly exercised behavior.
Global A+ is not established by this packet.
