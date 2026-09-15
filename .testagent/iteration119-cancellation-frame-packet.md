# Iteration 119 cancellation and child-operation packet

Status: corrected red-first baseline accepted; coherent product correction compiling.
The original active A+ goal and
hash-bound source-architecture order remain unchanged. Checkpoint
`4e81719a17cf32847c4d69add362f5ac5404b078` and its annotated intermediate tag were
verified on origin before this packet. Protected review/results trees are untouched.

## Research

The author personally reads all six current retry/redelivery ownership implementations,
the policy/context contracts, the new test cases and their full local fixtures. The
internal counterreview establishes NN-06 through NN-09; its source-derived claims
need executable regression evidence, not an assertion of external acceptance.

The consume fixture now accepts explicitly validated elapsed receive metadata so a
successful scheduling path can reach the actual ordinary/activity acknowledgment
boundary. No product behavior is supplied by this fixture and invalid task returns
remain visible. Policies are real Immediate/None policies wrapped at their public
custom-policy boundary. Pending work observes the token it actually receives and is
released/drained in finally; no sleeps or speculative timeout are the failure oracle.

The initial 61-case run yields 36 passes and 25 failures, but is not accepted as the
complete causal baseline: one nested case incorrectly cancels an already disposed
real policy context. The corrected case supplies a valid independently owned custom
decision token whose lifetime covers the outer fault notification. All other policy
cancellation cases still call the real active policy's Cancel method. The accidental
`UseExecuteAsync` test invocation is corrected to the actual configuration API
`UseExecuteAwaited`; neither correction changes a product expectation.

The corrected baseline exits 2 with exactly 25 causal failures: NN-06 eight, NN-07
ten, NN-08 five and NN-09 two. The earlier 35 cases plus the metadata contract pass
(36 passes, zero skips). No disposed-context failure remains. CTRF:
`/private/tmp/vsb-iteration119-cancellation-frame-causal-red-corrected/cancellation-frame-causal-red-corrected.ctrf.json`,
SHA-256 `55cf05fee2a035568d8f84d884ef4aeac52069d94a7118fbe7ffe1a9b2c8d216`.
All product files remain at checkpoint bytes for this run. Focused builds after the
two author-only fixture corrections complete with zero warnings and errors.

## Plan and concrete requirement map

| Finding or fixture contract | Regression and oracle |
|---|---|
| NN-06: source/decision cancellation reaches scheduling and acknowledgment | `PendingRedeliveryLifecycle_ReceivesSourceOrPolicyCancellationWithoutSchedulingAgainAsync`, 8 cases, both delivery families/stages/cancellation owners; pending argument token actually cancels, exact original exception token, one business attempt/schedule, correct acknowledgment count, acquired cleanup |
| NN-07: direct initial/attempt/nested fault notifications are cancellable | `PendingDirectFaultCallback_ReceivesSourceOrSelectedPolicyCancellationAsync`, 10 cases including both redelivery families; actual callback token cancels, exact original token, one callback and policy acquisition/cleanup, no business restart |
| NN-08: terminal getters remain infrastructure-owned | `TerminalDecisionGetterFailure_DoesNotRestartBusinessWorkAsync`, 4 families, exact failure instance, one factory/failing getter/cleanup and unchanged outer observer sequence |
| NN-08: actual context admission is guarded, not just earlier validation | `InvalidActualRetryContext_IsRejectedWithoutConsumingAnOuterBusinessBudgetAsync`, exact null-context diagnostic, one business attempt/factory/cleanup |
| NN-09: fresh child invocations do not inherit an active parent's prior child failures | `IndependentChildOperations_UnderAnActiveParentDoNotSuppressLegitimateBusinessRetryAsync`, sequential caught child and parallel barrier cases, exact first lifecycle failure and two legitimate second-child attempts/one effect, one parent attempt |
| Explicit consume-boundary metadata is accurate and validated | `ConsumeBoundaryElapsedTime_IsExactAndRejectsNegativeValues`, exact 7-tick metadata and exact negative-argument name |

All six method projections are registered manually in CoreRequirements.json. Keep
all earlier 35 ownership cases and 76 retry cases intact. Repeat the corrected
61-case baseline against unchanged checkpoint product source before any product edit.

The coherent correction passes the complete 61-case packet and the unchanged 76-case
suite, both zero failures/skips. The expanded manual proof adds twelve successful or
failed ordinary/activity scheduling/acknowledgment cases (including foreign-token
cancellation and cancellation after acceptance) and twelve resource-lifetime cases
covering single/nested policies across success, business, observer, factory, cleanup
and ordered compound failure. Their two exact requirement tuples are registered by
hand. The test-only internal-access seam observes private ownership storage under
its actual lock; no product telemetry or API is added solely for testing. It counts
retained completed entries too, so omitting removal cannot appear as success.

## Intended coherent implementation

Use the framework CancellationTokenSource linking primitive for source plus selected
decision cancellation, normalize requested cancellation to its original token, and
forward it through the real scheduling/acknowledgment/fault calls. Check cancellation
before each stage; genuine scheduling failures retain their original transport wrapper
and both failure identities. Guard actual decision reads/publication as infrastructure.

Each policy invocation receives fresh child ownership. Context/projection leases
within that invocation share ownership; only an actually escaping exact owned failure
or terminal decision is transferred to an already active parent association. Fresh
siblings never inherit the parent's earlier failure set. Preserve normal nested and
typed budget propagation and ordered primary/cleanup aggregation.

After the coherent correction: narrow green tests, old 76 cases, full Core, separate
single-cause mutations with verified source restoration, frozen internal read-only
counterreview and proportional acceptance/coverage gates. No executable-input edits
while build/test/format/review is running; no source/test/comment generator and no
final iteration/whole-product A+ assertion before the remaining gates succeed.
