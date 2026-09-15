# Iteration 119 projected payload and factory cleanup packet

Status: expanded91/23/76 and full Core3,805 pass, zero skip; independent compilable
terminal/order counterchanges are killed and byte-restored. Strict restored build,
Product/Unit format and frozen internal review pass within the local correction
scope. Further owned-scope/provider/foundation gates remain open; not final119/A+.
The token-getter follow-up reviewer accepts the original correction and establishes
the new terminal-payload counterexample without executing it; author proves it below.

## Research and acceptance map

The author personally reads the complete RetryOperationState, corrected RetryFilter,
RetryPolicyExecution, both ConsumeContextRetryPolicy variants and both policy-context
variants. The complete ConsumeContextRetryPolicyTests and RetryFilterTestFactory are
read as well. Source remains human-reviewed; no source/test/comment generator.

`TerminalPayloadFailure_PreservesInfrastructureAndCleanupWithoutOuterBusinessReplayAsync`
will use a real independent policy projection, with an exact once-only payload lookup
failure after the guarded ownership lookup. Initial and retry business paths, with
and without secondary cleanup failure, must retain the exact infrastructure identity,
ordered compound identities, one/two business attempts, zero effects, no outer
business classification, once-only factories/cleanup and released ownership storage.

`FailedPolicyAcquisition_PreservesPrimaryAndCleanupAndReleasesRegistration`
will cover typed/untyped admission-getter failures, typed initial projection failure,
and both mismatched policy representations. Successful underlying acquisition followed
by failing admission/projection and cleanup must preserve both failures in order,
dispose the acquired resource once and release any established cancellation registration.
Representation failures must retain their precise generated diagnostic and exact
secondary cleanup identity; caller-provided failures must retain both exact instances.

## Plan

Add four payload cases and five factory cases before changing product. Execute the
expanded suites against the current product; distinguish causal failures from
positive compound guards, and require all previous cases to pass. Protect terminal
lookup as synchronous infrastructure
on the input operation. Share ordered failed-factory cleanup semantics between the
two consume-policy projections. Repeat expanded suites, full Core, controlled
compilable counterchanges, formatting and frozen internal counterreview. Record any
new adjacent findings explicitly rather than broadening a local acceptance claim.

Known actual transport-provider publish/topology cancellation remains open and is
not replaced by local scheduling boundary stubs or this packet's acceptance.

Pre-execution oracle review corrects the retry-path NextDecisionCalls expectation
to zero: initial selection uses PolicyContext.CanRetry, whereas the terminal lookup
failure must precede Decision.CanRetry. The initial compile succeeds; this test-only
correction is compiled before the first execution. No failed run is counted as a
causal red proof for an incorrect test oracle. The two compound payload cases also
provide positive guards for the already-owned combined cleanup failure; they need
not fail against the prior unguarded terminal lookup.

## Executed red

Ownership91 cases:89 pass, exactly both non-compound terminal lookup cases fail
because no exception is thrown, zero skip. Old87 and both positive compound cases
pass. Raw `/private/tmp/vsb-iteration119-terminal-payload-causal-red/terminal-payload-causal-red.ctrf.json`,
SHA-256 `74ccb17ce0b879fbbb8f5b3e4fb35ebb30649fcaa677fd31858ed0a86d988e18`.
Factory23 cases: old18 pass, exactly all five new cases fail because cleanup alone
escapes instead of AggregateException, zero skip. Raw
`/private/tmp/vsb-iteration119-consume-factory-causal-red/consume-factory-causal-red.ctrf.json`,
SHA-256 `0cd04672c064669b8d6def0a2917edd6248273eff0f3447897fe26d87124ae5f`.
These seven failures are valid executed causal proofs, not fixture errors.

The immutable-checkpoint admission/mark/transfer counterreview establishes three
additional, unexecuted payload callback failure classes. They form a distinct next
ownership-frame packet: projection/child admission, redundant Mark callback and
redundant Propagate callback. Neither this correction nor the previous green Core
run is a closure claim for those paths. Actual provider cancellation also stays open.
