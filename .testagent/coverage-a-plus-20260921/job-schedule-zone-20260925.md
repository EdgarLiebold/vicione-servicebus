# Job schedule time zone calculation

Change commit: `fee047f53`. The full 971-line `JobStateMachine.cs` and
552-line lifecycle test were read before editing. The time zone selection in
`CalculateNextStartDate` is now a dedicated helper, preserving the original
UTC default, required settings payload, resolver invocation, and order relative
to one-time schedule handling and empty-cron cleanup.

The existing lifecycle test now also checks a new one-time start, a concrete
09:00 local occurrence in UTC+2 mapping to 07:00 UTC, and unchanged-result
idempotence. A deliberate product mutation returning UTC after resolver lookup
made the test fail with 09:00 UTC instead of 07:00 UTC. Source and built
JobService assembly were restored byte-for-byte, then the targeted test
passed. Read-only adversarial review found no P1/P2 issue. DST boundary logic
was untouched and has separate existing CronExpression tests.

JobService and Core tests built with zero warnings or errors. The full Core
run passed 6,438/6,438, zero failed or skipped, with Microsoft CodeCoverage.
Report: `artifacts/coverage-job-saga-zone-20260925/coverage.cobertura.xml`,
SHA-256 `431598a73f53a1c36558099e3ca23196b53d2192ed7911b3c79115f1abfeca68`.
The JobService assembly reports 95.9228% line and 90.1289% branch coverage.
`CalculateNextStartDate` reports 26/26 lines, complexity and CRAP 28 (previous
CRAP 32). `ResolveScheduleTimeZone` reports 5/5 lines, complexity and CRAP 4.

The global A+ goal remains open pending a complete provider profile and
isolated exact-commit build receipt. These Core and JobService numbers are
local evidence, not a whole-product grade.
