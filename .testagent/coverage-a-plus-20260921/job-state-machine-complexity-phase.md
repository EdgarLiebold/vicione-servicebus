# Job state machine configuration complexity

## Baseline and refactoring

The last complete 36-report profile at exact source/test commit `f7d924f94`
measured the `JobStateMachine` constructor at 167/167 lines and 104/104
reported branches. Full coverage left CRAP at its complexity floor of 104.
This was a structure hotspot rather than a test gap.

Exact source commit `74d278654` splits the existing constructor body into
eleven private lifecycle configuration methods. The constructor invokes them
in the original order:

1. events, schedules, and instance states;
2. submission and slot allocation;
3. active attempt lifecycle;
4. terminal attempt events;
5. stale attempts, progress, and checkpoints;
6. state query;
7. cancellation;
8. retry;
9. manual execution and finalization;
10. recurring submissions;
11. state-entry activities and finalized completion.

No existing event, schedule, state, binder, transition, or action expression
changed. The unified diff adds constructor calls, private method boundaries,
and method signatures only. `SetCompletedWhenFinalized` remains the final
registration. No production test was added because the existing lifecycle and
attempt-generation tests already execute every extracted configuration path;
a structural test would only freeze method layout.

Independent read-only adversarial review compared the registration sequence
and returned PASS. It found no missing, duplicated, or reordered registration,
no initialization dependency change, and no API, reflection, or serialization
effect. The class remains internal and the new methods are private; the state
machine's discovery logic inspects its state/event properties and backing
fields rather than private methods.

## Verification and measurement

- Current-byte Release Unit/Architecture solution build: zero warnings and
  zero errors.
- Current-byte complete Unit/Architecture gate: 10,096/10,096 passed, zero
  failures and skips.
- Current-byte Core with canonical Microsoft CodeCoverage: 6,329/6,329 passed.
  Report:
  `artifacts/coverage-job-state-machine-20260922/core-current.cobertura.xml`,
  SHA-256 `7a7218ab094a0b650af670f4cf2747f9e82efa09b27769c80a2c4e5a9a95d23a`.
- A clean detached checkout of exact commit `74d278654` passed locked restore,
  a zero-warning Release Core-test build, and 6,329/6,329 tests with canonical
  Microsoft CodeCoverage. Report:
  `artifacts/coverage-job-state-machine-20260922/exact-74d278654.cobertura.xml`,
  SHA-256 `3032fae27b4cd80f82ed5da056d9637e0a3c3cc5fb3d650272e1cefe43ac3496`.

| Exact-commit target method | Lines | Reported branches | CRAP |
| --- | ---: | ---: | ---: |
| constructor | 13/13 | 100% | 1 |
| `ConfigureEventsAndSchedules` | 18/18 | 100% | 16 |
| `ConfigureSubmissionAndSlotAllocation` | 25/25 | 100% | 6 |
| `ConfigureAttemptLifecycle` | 27/27 | 100% | 18 |
| `ConfigureTerminalAttemptEvents` | 12/12 | 100% | 26 |
| `ConfigureStaleAttemptsAndUpdates` | 16/16 | 100% | 14 |
| `ConfigureStateQuery` | 6/6 | 100% | 1 |
| `ConfigureCancellation` | 30/30 | 100% | 10 |
| `ConfigureRetries` | 16/16 | 100% | 1 |
| `ConfigureManualExecutionAndFinalization` | 13/13 | 100% | 1 |
| `ConfigureRecurringSubmissions` | 8/8 | 100% | 8 |
| `ConfigureStateEntryActivities` | 5/5 | 100% | 6 |

The twelve target methods total 189/189 lines with full reported branch
coverage; maximum CRAP is 26. This claim is intentionally limited to the
constructor and named configuration methods. Compiler-generated lambda methods
in the same state-machine class retain pre-existing branch gaps and remain in
the product-wide campaign.

The mandatory Microsoft `coverage-analysis` and `crap-score` workflows guided
the hotspot selection and quantitative result, and `run-tests` governed the
.NET 10 Microsoft.Testing.Platform commands. The source changed after the
complete `f7d924f94` profile, so a fresh multi-project profile is required
before updating global totals; global A+ remains open.
