# C31 mutation validation

Every mutation was applied in isolation to the accepted C31 candidate, exercised through the native
xUnit 4 / Microsoft.Testing.Platform path, and then reverted before the next mutation. Every test
command returned exit code `2`; the final restored candidate returned exit code `0`.

| Mutation | Required protection | Rejecting native test |
|---|---|---|
| Remove the `TimeProvider` from the polling delay | Virtual time is the only wait clock | `StatusChange_IsObservedOnlyAtTheExactVirtualPollBoundary` |
| Return the last result instead of throwing at timeout | Timeout cannot look like success | `ZeroTimeout_ObservesOnceAndThenReportsTheUnexpectedResult`, `Timeout_ThrowsTypedFailureWithTheCompleteLastObservation` |
| Observe health before checking pre-cancellation | Cancellation owns the first boundary | `AlreadyCanceledWait_DoesNotObserveMutableBusState`, `AlreadyCanceledCollectionWait_DoesNotObserveAnyBus` |
| Check the expected state only after the timeout branch | Exact-deadline success remains valid | Immediate, exact-deadline and convenience-overload cases |
| Reverse the collection before starting waits | Result order equals input order | `CollectionWait_StartsEveryBusAndReturnsCompleteResultsInInputOrder` |
| Remove the null-element guard | Invalid collections cannot partly start | `NullCollectionElement_IsRejectedBeforeStartingAnyWait` |
| Change the private polling interval from 100 ms to 99 ms | Poll cadence is exact and deterministic | `StatusChange_IsObservedOnlyAtTheExactVirtualPollBoundary` |
| Change one passive requirement variant | Requirement projection has no missing or extra carrier | `RequirementCoverageProjectionTests` |
| Allow a health observation after the deadline | Late state cannot revive an expired wait | `ExpectedStatusReachedOnlyAfterTheDeadline_DoesNotTurnTheExpiredWaitIntoSuccess` |
| Remove collection status validation | Invalid public state fails before any wait starts | `UndefinedCollectionExpectedStatus_IsRejectedBeforeStartingAnyWait` |
| Drop the caller token from collection waits | Collection cancellation is causal and exact | `AlreadyCanceledCollectionWait_DoesNotObserveAnyBus` |
| Enumerate the input twice | Deferred collections are materialized exactly once | `CollectionWait_EnumeratesTheInputExactlyOnce` |

Representative raw MSBuild/MTP evidence is retained under
`artifacts/build-diagnostics/c31/`. The final restored full-profile evidence is bound separately in
`VALIDATION.md`; no mutant remains in the source tree.
