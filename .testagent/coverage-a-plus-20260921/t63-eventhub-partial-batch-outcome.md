# T63 — Event Hubs partial-batch outcome

T59 remains the latest complete product-wide coverage and CRAP baseline. T63
closes the partial-batch risk recorded in T62: when an earlier SDK batch has
completed and a later batch fails, a host retry must send only unresolved
messages.

| Contract | Direct oracle |
| --- | --- |
| Route and size splitting confirm exact contiguous ranges | `EventHubProducerBatchSenderTests.LaterProviderFailure_PreservesOnlyEarlierConfirmedContextAsync`: two messages in the first successful provider batch and one in the failed second batch; confirmation flags and batch disposal checked for both splits |
| Cleanup cannot change provider outcome | `EventHubProducerBatchSenderTests.DisposeFailure_AfterConfirmedBatch_DoesNotResendOrAbortPendingBatchAsync` and `DisposeFailure_AfterProviderFailure_PreservesPrimaryExceptionAsync` |
| Retry excludes confirmed contexts | `EventHubProducerOutcomeTests.PartialBatchFailure_RetriesOnlyPendingMessageAndPreservesObserverOutcomesAsync`: exact submissions `[1,2]`, `[2]`, Pre/Post/Fault indices and original exception |
| Terminal retry retains exact later failure | `EventHubProducerOutcomeTests.TerminalRetryFailure_KeepsConfirmedMessageOutOfSecondAttemptAsync` |
| Secondary observer/logger failures do not replay confirmed messages | `EventHubProducerOutcomeTests.PartialRetry_SecondaryObserverAndLoggerFailuresCannotReplayConfirmedMessageAsync` |
| A later failure cannot mark a confirmed message span as failed | `EventHubProducerOutcomeTests.PartialBatchFailure_DoesNotMarkConfirmedMessageActivityAsFailedAsync`: fixed message ID, `Ok` status, no exception event |

The route/size sender tests failed on old product behavior because the first
confirmed contexts were unmarked (`/private/tmp/vsb-t63-sender-red.log`). The
retry test failed with replay `[1,2]`, `[1,2]`
(`/private/tmp/vsb-t63-producer-red.log`). The telemetry case failed on the
new partial-batch implementation before its correction: the first confirmed
message span had `Error` status (`/private/tmp/vsb-t63-telemetry-red.log`).
These failures tie the new oracles to the defects.

The final affected-project build has zero warnings and errors
(`/private/tmp/vsb-t63-build-final.log`). Focused sender and producer suites
pass 15/15 and 12/12 without skips (`/private/tmp/vsb-t63-sender-final.log`,
`/private/tmp/vsb-t63-producer-final.log`). The existing real Event Hubs
delivery control passed 3/3 in a fresh Event Hubs/Azurite fixture with empty
teardown findings (`/private/tmp/vsb-t63-eventhub-delivery.log`). The final
read-only Red Team review accepts the exact confirmation ranges, pending-only
retry, cleanup isolation, exception identity and telemetry ownership with no
remaining concrete blocker. Its review was static; the executed test results
above are separate evidence.

The controlled producer-context test proves host-like retry behavior without
claiming that the emulator can deterministically force a split-batch provider
failure. No new global Line, Branch or CRAP figure is claimed from this packet.
The agreed full 33-profile measurement follows T60–T63.
