# T62 — send observer outcome ownership

T59 remains the latest complete 33-profile product-wide coverage and CRAP
baseline. T62 follows one physical send through the common transport and
Event Hubs single/batch producer orchestration. The Microsoft Roslyn pairing
was run once for source selection (`/private/tmp/vsb-t62-pairing.json`); its
static source references are not runtime coverage evidence.

| Contract | Direct oracle |
| --- | --- |
| Confirmed Core send remains successful if `PostSend` fails | `SendTransportTests.PostObserverFailure_AfterProviderSubmission_DoesNotMakeDeliveredSendRetryableAsync`: one provider submission; exact create, pre, send, post trace; no false fault |
| Core primary provider result survives throwing logger | `SendTransportTests.ThrowingLogger_DoesNotChangeConfirmedSendOrMaskProviderFailureAsync`: success and exact provider exception identity, with respective post/fault trace |
| Confirmed Event Hubs single/batch send remains successful if `PostSend` fails | `EventHubProducerOutcomeTests.PostObserverFailure_DoesNotRetryConfirmedMessagesOrReportSendFaultAsync`: one provider call, exact message indices, all pre/post callbacks including the second batch context, zero fault callbacks |
| Event Hubs provider failure remains authoritative if `SendFault` fails | `EventHubProducerOutcomeTests.ProviderFailure_RemainsTheExactFailureWhenFaultObserverAlsoFailsAsync`: exact primary exception object, one provider call, fault callback per message, no post callback |
| Event Hubs logger failures cannot change single/batch outcome | `EventHubProducerOutcomeTests.ThrowingLogger_CannotTurnConfirmedSendIntoFailureOrMaskProviderFailureAsync`: success/failure × single/batch, one provider call, exact submitted indices and fault/post separation |

The Core post-observer test failed red before the fix with the exact observer
exception after confirmed provider submission
(`/private/tmp/vsb-t62-core-red.log`). The four Event Hubs theory cases failed
red before the fix: Post errors escaped and fault observer errors replaced the
provider failure (`/private/tmp/vsb-t62-eventhub-red.log`). The fix confines
send-fault classification to pre/admission/provider work and makes confirmed
send diagnostics, post callbacks, fault callbacks and secondary logging best
effort. Read-only adversarial re-review found no concrete blocker in this
bounded correction. The Event Hubs outcome tests use a controlled transport
context and do not claim real Azure SDK batch splitting or broker delivery;
the complete existing broker project is an adjacent regression control.

The final Core build has zero warnings/errors and its full suite passes
6,897/6,897 without skips (`/private/tmp/vsb-t62-core-full.log`). The Event
Hubs build has zero warnings/errors and its focused outcome suite passes 8/8.
An initial broker attempt omitted the separate `azurite` argument, so the
dynamic Blob port was not projected. With both services, the first full run
exposed a startup race: Compose reported a running container before the
emulator finished creating its entities. The fixture runner now waits for the
emulator's own bounded readiness signal. The next full run passed 96/97;
one existing checkpoint test timed out after 30 seconds waiting for consumer
entry (`/private/tmp/vsb-t62-eventhub-full-final.log`). That exact test passed
1/1 in a new ready fixture (`/private/tmp/vsb-t62-eventhub-checkpoint-retry.log`).
Both accepted fixtures have empty findings and collected Event Hubs/Azurite
logs. This is a 96/97 full run plus an isolated 1/1 retry, not a clean 97/97
single-run claim.
Final read-only Red Team review found and then rechecked one fixture issue:
the Docker log poll originally had no subprocess timeout despite its outer
deadline. Every poll now passes its remaining budget to `subprocess.run` and
turns timeout into a bounded fixture error. A controlled timeout probe passed;
the final re-review reports no concrete blocker.

Known next contract: when an Azure SDK provider confirms an earlier partial
batch and a later partial batch fails, the current Event Hubs producer still
faults every message and the host retry can resend confirmed messages. This
remains open for a separate progress-aware batch packet. No T62 global Line,
Branch, or CRAP figure is claimed from the focused/affected checks.
