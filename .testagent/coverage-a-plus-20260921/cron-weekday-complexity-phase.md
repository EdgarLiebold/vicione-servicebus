# Cron weekday progression complexity

The exact product profile at `a0931a6bb` measured
`CronExpression.ProgressNextFireTimeDayOfWeek` at CRAP 53.38 (69/75 covered
lines, complexity 52). Its four mutually exclusive calendar rules now have
separate methods for the last weekday, nth weekday, week interval, and
ordinary weekday. The dispatch preserves the former rule order, date
calculations, and restart decisions.

The published next-fire contract has two additional product-level cases:
the last Friday in December 2026 advances to January 29, 2027, and the
fifth Friday after January 31, 2025 skips February, March, and April before
returning May 30. Both cases passed against the original implementation
before the extraction. They also pass against the extracted implementation.

Microsoft CodeCoverage Cobertura from the full Core suite:
`artifacts/coverage-cron-dayofweek-20260924/raw/core/coverage.cobertura.xml`,
SHA-256 `ae9ca5f6ed7fc09bc7bbe2b37032c0d4d343c667e0983cc3f18b5ed76704e592`.
The current-byte Core suite passed 6,381/6,381 with zero failures/skips,
both normally and under coverage. An independent read-only adversarial
review compared all four paths and returned PASS without a concrete
regression finding.
The Release Unit/Architecture gate passed 10,245/10,245 with `--no-build`.
The Release solution build with the required local execution permission
completed in 47 seconds with zero warnings and errors.

| Method | Covered lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| `ProgressNextFireTimeDayOfWeek` | 7/7 | 6 | 6 |
| `ProgressLastDayOfWeek` | 20/21 | 12 | 12.02 |
| `ProgressNthDayOfWeek` | 24/24 | 14 | 14 |
| `ProgressEveryNthWeek` | 15/16 | 8 | 8.02 |
| `ProgressOrdinaryDayOfWeek` | 20/20 | 12 | 12 |

These are focused method scores from one Core report. The product-wide
line, branch, and CRAP totals remain those of `a0931a6bb` until another
exact-commit, all-assembly profile is collected. Global A+ remains open.

Earlier sandboxed `dotnet build` attempts stalled in MSBuild's
`_GetProjectReferenceTargetFrameworkProperties` and exited without a compiler
diagnostic. Their logs and a diagnostic binlog remain under
`artifacts/coverage-cron-dayofweek-20260924/`. The same Release solution
build succeeded with local execution permission, matching the permissions
used for the successful .NET test runs. This is evidence of an execution
environment limitation rather than a compiler failure in the change.
