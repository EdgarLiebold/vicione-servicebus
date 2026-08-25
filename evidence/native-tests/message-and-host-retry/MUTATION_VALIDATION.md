# Message and host retry — mutation validation

## Method

Each mutation changed one production behavior at a time. The affected project was rebuilt in
Release, only the owning test was executed to establish causality, and the original production code
was restored before the next mutation. An experiment that did not make its intended test fail is
listed separately and is not counted as evidence.

## Effective mutations

| Mutated behavior | Owning test and observed failure |
|---|---|
| Remove automatic `RetryBusObserver` attachment from bus-level message retry | `StoppingTheBus_CancelsAPendingRetryWithoutStartingAnotherAttempt` timed out because the pending one-hour retry delay was not cancelled by bus stop. |
| Give caller cancellation precedence over transport stopping after a linked delay is cancelled | `Retry_WhenBothSourcesAreCancelledReportsTheStoppingTransport` received `OperationCanceledException` instead of the required `ConnectionException`. The test cancels both sources during a real pending retry, not before entry. |
| Dispose `RetryBusObserver` without cancelling its stable stopping token first | Both lifecycle tests observed an uncancelled token after stop/fault termination. |
| Wrap the last host-transport failure before reporting transport stop | `Retry_StoppingDuringBackoffProducesAConnectionFailureWithTheLastTransportFailure` failed `Assert.Same`; the exact causal failure had been replaced by a wrapper. |

All four mutations were rejected for their intended reason. The final source contains none of the
mutations.

## Excluded experiment

Reversing only the two preflight cancellation checks did not fail the test because the later catch
block still applied the authoritative stop-first classification. It was restored immediately and is
not counted as mutation evidence. The experiment led to strengthening the dual-cancellation test so
that both sources cancel while the retry delay is active.
