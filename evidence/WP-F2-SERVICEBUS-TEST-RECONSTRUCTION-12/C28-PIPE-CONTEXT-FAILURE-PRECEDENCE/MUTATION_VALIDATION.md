# C28 mutation validation

Each product mutation was applied alone, built in `Release`, executed through the native xUnit v4
Microsoft Testing Platform executable and reverted before the next mutation. The final product
source was then rebuilt and rerun.

| Mutated product invariant | Detecting test and observed result |
|---|---|
| Let fault-notification failure escape | `OperationFailure_WhenFaultNotificationFails_PreservesTheExactOperationFailure` failed with `CleanupFailureException` instead of the operation failure. |
| Let stop failure escape | `OperationFailure_WhenStopFails_PreservesTheExactOperationFailure` failed with `CleanupFailureException` instead of the operation failure. |
| Let disposal failure escape | `OperationFailure_WhenDisposeFails_PreservesTheExactOperationFailure` failed with `CleanupFailureException` instead of the operation failure. |
| Omit fault notification | `OperationFailure_ReportsFaultThenStopsThenDisposes` failed because the observed failure was null. |
| Omit stop | `SuccessfulOperation_StopsThenDisposesWithoutReportingAFault` failed on exact lifecycle order. |
| Omit disposal | `SuccessfulOperation_StopsThenDisposesWithoutReportingAFault` failed on exact lifecycle order. |
| Replace the caller cancellation token with `CancellationToken.None` | `SuccessfulOperation_StopsThenDisposesWithoutReportingAFault` failed on exact token identity. |
| Omit the operation invocation | `SuccessfulOperation_WhenEveryCleanupStepFails_RemainsSuccessfulAndRunsOnce` failed because the execution count was zero. |

All eight mutations failed for their intended single cause. The restored focused run passed 8/8.
