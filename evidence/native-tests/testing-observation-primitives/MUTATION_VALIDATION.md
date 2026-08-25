# Testing Observation Primitives — Mutation Validation

Date: 2026-08-24

Each product mutant was compiled before its owning native test was run. Mutants that removed a wakeup
or replaced virtual time with process time were intentionally bounded at the test-process boundary;
the normal owning test finishes in under two seconds, while the mutant remained blocked until the
Lead aborted it. Such a hang is a rejected messaging-test regression, not a skipped or green result.
Every mutant was removed before the final runs.

| One-cause mutation | Owning verdict | Result |
|---|---|---|
| Replace the configured asynchronous deadline with an immediate deadline | `MessageAddedAfterObservationStarts_CompletesTheObservation` | Exit 2; the observation was already complete before the later message could be added. |
| Replace the asynchronous list's injected provider with `TimeProvider.System` | `AdvancingTheConfiguredTimeout_ReturnsFalseWithoutWallClockWaiting` | Product and test compiled; advancing virtual time no longer completed the observation, so the test remained blocked until the bounded mutation process was aborted. |
| Replace the synchronous provider-backed deadline with `TimeProvider.System` | `SynchronousSelection_UsesTheSameVirtualTimeout` | Product and test compiled; the case never observed its test-owned timer and was aborted as a deterministic hang. |
| Complete the observation channel instead of writing the newly added element | `MessageAddedAfterObservationStarts_CompletesTheObservation` | Exit 2; the observation returned `false` instead of receiving the later message. |
| Stop cancelling the inactivity token when inactivity is forced | `ForcedBeforeTaskMaterialization_CompletesImmediately` | Exit 2; task completion remained true but the exact public token state was false. |
| End the observer loop after its first interval without querying the source | `EachElapsedVirtualInterval_QueriesTheSourceExactlyOnce` | Product and test compiled; the required second timer was never created and the case was aborted as a deterministic hang. |
| Replace the inactivity observer's injected provider with `TimeProvider.System` | `EachElapsedVirtualInterval_QueriesTheSourceExactlyOnce` | Product and test compiled; the test-owned provider never observed the first interval timer, so the test remained blocked until the bounded mutation process was aborted. |
| Restore the broad `catch (Exception)` around the observer loop | `SourceFailure_IsExposedByTheObservationTask` | Exit 2; the exact source failure disappeared and no exception reached the caller. |
| Remove the harness-completion token from the asynchronous linked source | `CompletionToken_EndsObservationWithoutReturningAnElement` | Product and test compiled; cancellation no longer ended the observation and the case was aborted as a deterministic hang. |
| Evaluate synchronous filters while holding the message-list monitor | `SynchronousFilter_DoesNotBlockAConcurrentProducer` | Product and test compiled; the dedicated producer could not enter `Add` and the case was aborted as a deterministic deadlock. |
| Remove the synchronous producer-contract row from `CoreRequirements.json` | `CoreRequirements_MatchCompiledRequirementMetadata` | Exit 2; the compiled fact was reported verbatim as absent from the embedded projection. |

No mutation survived. The manual aborts are confined to deliberately deadlocking mutants; no final
or ordinary test run uses a wall-clock threshold as its product oracle.

Verdict: **PASS**.
