# T88 Courier successor outbox result and send admission

The T85 union still has unexecuted result-forwarding paths in
`InMemoryOutboxExecuteContext<TArguments>`. Existing Courier tests exercise
result shapes on a host context and an execute retry with a revised itinerary,
but did not prove the successor activity's own result and send are held by its
outbox. T88 extends that real broker journey instead of invoking forwarding
methods solely for coverage.

The revised route's successor now sends a `SuccessorEffect` and waits before
completing. Before its release, the test proves that the effect has neither
reached the bus `PreSendAsync` observer nor a consumer and that the routing
slip has not completed. After release, it requires exactly one admitted and
one consumed effect with the right tracking number and value. The successor
uses `CompletedWithVariables` with either an object or an enumerable of
key/value pairs; the completed slip must contain the final value and must no
longer contain the deliberately stale `RemoveMe` variable. The termination
control proves that a discarded successor never executes or sends and leaves
the stale variable intact. The original retry assertions still prove that
only the selected execute attempt's two effects reach the bus.

Read-only Red Team review first found two P2 gaps: the successor outbox had
no outgoing operation, and the old `concurrentDelivery` rows used only one
execute context and could not distinguish concurrent behavior. An outgoing
successor effect with a release gate closed the first gap. Removing the
unproved axis left three distinct route/result cases. Final re-review was
PASS with no remaining concrete P1/P2 issue. An isolated counterchange removed
only `UseVolatileOutbox()` from the successor. The two revise cases then
failed at the pre-release `AdmittedSuccessorEffects` assertion, while the five
other class cases passed. The test file was restored byte-for-byte to SHA-256
`b23eece30de982a834f95375927f598e0e88fd143801ecce6da541f874d25182`.

The exact test commit is `798d1e99e`. The focused Courier journey class
passes 7/7, and the complete Core xUnit v3/MTP project passes 6,947/6,947
with no failures or skips on that commit. The count is one lower than T87
because redundant concurrent-delivery rows were removed; the behavior oracle
is stronger. No product source changed. T85 remains the latest complete
33-profile product-wide Line/Branch/CRAP measurement; global Line and Branch
A+ remain open. T88 is the third focused packet after that checkpoint.
