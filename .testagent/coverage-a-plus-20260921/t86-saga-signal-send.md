# T86 awaited saga signal send

The T85 product-wide profile identified uncovered callback-send entry points.
The existing callback journey tests already cover data events, deferred factories,
callback faults, transport faults and separate saga owners. T86 adds a distinct
integrated contract for a data-free saga signal whose message is supplied by a
pending `Task<T>`.

`SignalSend_AwaitsMessageBeforeCallbackAndNeverSendsAFailedMessageAsync` runs
both outcomes through an in-memory bus and real saga state machine. Before the
task is released, it proves that no callback, send, delivery or continuation has
occurred. On success it checks the exact delivered payload, destination,
correlation and initiator IDs, callback header, trace order and persisted
terminal state. On message creation failure it checks the preserved root cause
through the nested event wrappers, no callback, no send or delivery, and the
persisted `Ready` state.

During test development, a synchronous `.Then` accepted a Task-returning
lambda and discarded its completion. The final test uses `.ThenAwaited` for the
nested signal, so it observes the full execution. The read-only Red Team then
found a missing requirement projection and cancellation-sensitive harness
cleanup; both were corrected. Its final verdict was PASS, with no concrete
remaining P1/P2 issue. It found that the test distinguishes early or missing
callbacks, wrong message or routing metadata, early continuation, and sending
or continuing after message creation fails.

The exact test commit is `11028352e`. The focused theory passes 2/2. The full
Core xUnit v3/MTP project passes 6,946/6,946 without failures or skips on the
same commit, including the requirement projection gate. No product source was
changed. T85 remains the latest complete 33-profile product-wide Line/Branch/
CRAP measurement; global Line and Branch A+ remain open. T86 is the first
focused packet after that checkpoint.
