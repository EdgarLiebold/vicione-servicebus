# T60 — persistent job terminal and slot lifecycle

T59 is the frozen product-wide baseline: 13,287 passed tests, 91.73582% physical
lines, 84.36423% conservative branches and no method with CRAP above 30. Source
code is unchanged in T60. The T58 Roslyn static pairing report was reused because
the source tree is unchanged; it is only a source/test reference heuristic, not
coverage evidence. The T59 receipt/XML analysis and existing provider integration
tests identified a connected gap: terminal outcomes on independent persistent
jobs and whether the single execution slot becomes available after each outcome.

## Acceptance map

| Contract | Azure Table test | EF Core/PostgreSQL test |
| --- | --- | --- |
| Two genuine overlapping consumer invocations keep success and fault separate | `ConcurrentJobs_PersistOnlyTheirOwnCompletionOrFailureAsync` | `ConcurrentJobs_PersistOnlyTheirOwnCompletionOrFailureAsync` |
| Completion releases a held slot to a waiting job | `CompletedRunningJob_ReleasesTheSlotForAWaitingJobAsync` | Existing single-job completion plus the other provider's explicit slot test; not claimed for EF |
| Fault releases a held slot to a waiting job | `FaultedRunningJob_ReleasesTheSlotForAWaitingJobAsync` | `FaultedRunningJob_ReleasesTheSlotForAWaitingJobAsync` |
| Cancellation releases a held slot to a waiting job | `CanceledRunningJob_ReleasesTheSlotForAWaitingJobAsync` | Existing single-job cancellation plus the other provider's explicit slot test; not claimed for EF |
| Azure Table observation timeout covers local provider latency | All eight Azure Table JobService cases | N/A |
| EF observation timeout covers local provider latency | N/A | All six EF JobService cases |

The slot tests hold the first real consumer, observe `IJobSlotUnavailable`
for the second ID, prove that only the first consumer ran, then release the
first job by its corresponding terminal action. The consumer records every
attempt rather than one entry per job ID. After both terminal events and fresh
database loads, each harness is stopped before exact publication snapshots.
The tests retain the original exception or cancellation reason and reject
cross-job terminal events. Requirement projections are updated in both suites.

The first T59 Azure Table local profile failed an existing fault observation at
the default short inactivity timeout. Its log shows the fault send just after
the observation ended. ETag 412 conflicts elsewhere in that profile belong to
concurrent Future tests; no causal link to the JobService observation is proved.
Both local JobService fixtures now use their validated local operation timeout
for test and inactivity waits. A first new mixed-outcome attempt with the
default job concurrency limit of one timed out waiting for the second job;
the distinct overlap test now configures limit two. The one-slot cases retain
limit one and use a one-second slot retry interval.

## Focused evidence and review

- Azure Table JobService class: 8/8 passed; full local project: 44/44 passed,
  zero skipped, isolated Azurite findings empty.
- EF Core/PostgreSQL JobService class: 6/6 passed; full local project: 96/96
  passed, zero skipped, isolated PostgreSQL findings empty.
- Read-only adversarial review found and then rechecked three oracle gaps:
  overlapping execution, exact attempt counts and cancellation gate cleanup.
  All were repaired. It reports no remaining concrete packet blocker and did
  not edit files or run tests.
- An isolated product mutation removed the Completed-state slot release from
  `JobStateMachine`. The new Azure Table completion-to-waiting test alone
  failed among 8 class cases (7 passed, 1 failed). The temporary worktree was
  removed; MAIN product source was never changed.

The broad test-generation workflow used the Microsoft `code-testing-agent`
research/plan/implement path. `find-untested-sources` supplied Roslyn pairing;
`test-gap-analysis` and `assertion-quality` guided the explicit gates,
per-invocation capture and terminal-event oracles. `run-tests` selected the
SDK 10/MTP/xUnit v3 class filters. `coverage-analysis` uses the strict 33-profile
receipt aggregate and independent XML audit. The frozen product-wide T60
measurement, its independent audit, changelog and publication are still pending.
