# Iteration 119 operation ownership packet

Status: 35/35 new cases causally fail against unchanged product source; the existing
76/76 cases pass. The corrected source passes 35/35 new and 76/76 existing
focused cases, then the complete Core host passes 3,744/3,744 with zero skips.
Both format verifications pass without source edits. Internal read-only counterreview
finishes and releases its source freeze, confirming local accepted axes but establishing
four adjacent open causal findings NN-06 through NN-09. Further mutation/acceptance
work remains open; see the owned evidence packet and complete counterreview report.

## Research

The immutable source-architecture task and active original goal remain the authority.
Checkpoint b6e54731692da2635cc5839e7c0af40d86a04044 is locally clean and previously
verified on origin with its explicitly intermediate nested-ownership tag. Protected
review/results trees are neither inputs nor edit/staging targets.

The author personally reads the complete current RetryFilter, RetryLifecycleFaults,
both redelivery filters, RetryFilterTestFactory, ConsumeContextProxy, ActivityContext,
RetryPolicyContext and RetryContext contracts, and the complete 478-line consume
fixture. Existing RetryFilter tests and their prior causal evidence are reused
unchanged. The internal reviewer established NN-01 through NN-04; it did not execute
the proposed counterexamples. These new tests provide the executable proof.

## Plan and requirement map

| Accepted requirement | Manual regression and exact oracle |
|---|---|
| NN-01: lifecycle failures do not enter another filter's business retry/redelivery | 16 ordinary/activity cases in both directions; exact failure, attempts, effects, observer sequence, zero scheduling and exactly-once acquired cleanup |
| NN-02: cleanup cannot replay committed work or hide a primary failure | Two success/after-retry cases plus three primary-and-cleanup cases; one effect, one acquired cleanup, exact sole failure or ordered exact primary/cleanup identities |
| NN-03: factory, classification and invalid-output failures remain infrastructure | Four injected factory/decision/classification cases plus six null-output cases; one factory/failing boundary, no outer restart, exact contract diagnostics and cleanup |
| NN-04: terminal ownership belongs to the active operation | Terminal observer failure followed by same-context/same-exception business retry; exact two later attempts/one effect, retained public diagnostic |
| Preserve diagnostics and caller-owned state | Two repeated-terminal cases; current owned diagnostic or unchanged caller-owned payload, both current observer notifications |
| Independent concurrent operation ownership | One deterministic entered/release barrier case; a held lifecycle failure cannot suppress another operation's legitimate business retry using the same context and exception instance |

All eight method projections are registered by hand in CoreRequirements.json.
The injected policy wraps real Immediate/None policies; deliberate invalid output
and lifecycle failures are test-only admission probes, not product dummy behavior.
The cross-filter tests use the existing consume boundary and an actual product
ConsumeContextProxy for the activity projection. No external provider execution is
claimed. Finally blocks drain held work; no timing-based sleeps or source generators
are used.

## Implementation order

Compile/run all 35 new cases against unchanged checkpoint product code, retaining
the existing 76-case suite. Then implement one coherent internal policy-operation
lifetime with distinct business and infrastructure failure ownership, exact cleanup
preservation, active terminal ownership and caller-safe public diagnostics. Apply
that lifetime consistently to ordinary retry and both redelivery families. Redelivery
policy selection, previous-delivery counting, successful scheduling and cancellation
must remain intact. Read every changed implementation/comment personally.

Repeat the new and existing tests, execute separate compilable causal mutations with
byte-for-byte restoration, freeze and request internal read-only counterreview, then
collect explicit-profile coverage/CRAP and run proportional complete gates. Never
edit executable inputs during a running build/test/format/review. Secure completed
coherent progress without claiming final iteration or entire-goal completion.
