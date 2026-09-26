# Five-gap follow-up to the 9676f7891 measurement

This packet adds behavioral regression protection for the five gaps observed
by the last complete measurement. It changes tests, not production source.
Closing each measured gap remains unproven until the new complete measurement.

Update: the [complete 90239365c measurement](product-wide-profile-90239365c.md)
now proves all five targeted line gaps closed. It supersedes pending measurement
notes below while preserving this pre-measurement packet's verification history.

## Requirements and concrete tests

| Contract | Test |
| --- | --- |
| Faulting an acquired RabbitMQ channel operation invalidates its shared owner before reuse and preserves the original exception | `SendAsync_ChannelOperationFailureInvalidatesTheOwnerBeforeTheNextUseAsync` |
| Late successful consumption cannot change paused or terminated KillSwitch state/counters; recovery counts only new successes | `LateSuccessfulCompletion_PreservesPausedOrTerminatedStateAndCountersAsync` (two rows) |
| Repeated cancellation returns exact missing-target data without double capacity release or damage to another message/store | `Cancel_RemovedScheduleReturnsNotFoundWithoutChangingNeighborsAsync` |
| Discard/requeue/abandon of a removed inbox target preserve same-message, same-consumer and other-store neighbors | `Inbox_RemovedTargetReturnsNotFoundForEveryOperatorActionWithoutChangingNeighborsAsync` |
| Real Courier execution carries exact tracking/processor/argument tags and caller/send/receive/process trace ancestry | `Execution_RecordsExactCourierIdentityAndPreservesTheCallerTraceAsync` |
| Successful empty in-flight SQS poll after stop ends without another provider poll or hidden shutdown warning/error | `Receiver_SuccessfulEmptyPollAfterStopCompletesWithoutPollingAgainAsync` |

## Review and deliberate negative controls

Each packet received a read-only adversarial review. The SQS review found that
successful Agent.Completed does not prove a fault-free consume loop: shutdown
catches and logs the exception. The final test installs the actual ambient
recording LogContext, rejects every Warning-or-higher entry after shutdown and
restores the previous context in finally. Follow-up review confirmed the fix.

| Isolated mutation | Observed rejection |
| --- | --- |
| RabbitMQ borrowed FaultedAsync forwarding replaced with CompletedTask | New owner-disposal assertion fails; 9 controls pass |
| KillSwitch RecordSuccess state guard removed | Both new rows change Paused/Terminated to Running and fail; 17 controls pass |
| EF NotFound replaced with valid but incorrect InvalidState/Pending/Pending | Both new tests fail; 11 controls pass |
| Courier execution tag callback emptied | New test fails on missing tracking tag after real successful execution |
| SQS exception thrown immediately after normal consume-loop exit | New test fails on recorded shutdown warning; 4 cancellation controls pass |

All mutations were manually reverted in the isolated checkout. Source diff
checks confirm no retained mutation. Restored scoped runs: RabbitMQ 10/10,
KillSwitch 19/19, EF 13/13, Courier 1/1 and SQS 5/5.

Full working-tree project checks passed: RabbitMQ 474/474, Core 6514/6514,
EF 312/312, SQS 296/296. Verify-only formatting passed for each touched C#
packet. These runs are not exact-commit receipts or a product-wide aggregate.

## Limits and outstanding gates

- RabbitMQ and SQS use controlled client seams, not physical broker evidence.
- KillSwitch uses real state logic with controlled observer calls and endpoint;
  the new rows target Paused/Terminated, not intermediate stopping/starting.
- EF uses real SQLite and recreated store instances, not process restart or
  other-provider proof; retained-field claims match the asserted projections.
- Courier uses the real InMemory execute path, not compensation or an exporter.
- Raw local logs live under artifacts/t24-* through artifacts/t28-* in MAIN
  and the isolated checkout. They are not published merely by this document.
- Exact-commit receipts, all provider profiles, updated coverage/CRAP inventory
  and remote checkpoint are still required. No A+ acceptance is claimed.

Microsoft code-testing-agent and run-tests guidance was applied. Existing
coverage inventory selected the scope; tests were reviewed against product
behavior and deliberately incorrect implementations, not just line counts.
