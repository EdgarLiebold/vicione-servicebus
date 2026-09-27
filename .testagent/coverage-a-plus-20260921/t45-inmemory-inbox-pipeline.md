# T45: InMemory reliable-inbox pipeline

Base: `148de7e49bba248fc5a2f74a0d4be1f97c5de1f3` (T44 audited and pushed).
Microsoft code-testing-agent focused workflow and run-tests remain applicable.
No full measurement until the entire coherent packet is implemented and reviewed.
Research and adversarial planning notes: local `artifacts/t45-candidate-research.md`.

## Static pairing and reading

Microsoft Roslyn analyzer executed once against a bounded byte-identical mirror
of the two context/factory files, integration tests, InternalAccess driver and
their projects. `artifacts/t45-pairing.log`, exit0: two sources, two test files,
zero paired, two unpaired. Suggested paths are under
`tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Providers/Persistence/ReliableMessaging/InMemory/`,
named `InMemoryReliableInboxContextTests.cs` and
`InMemoryReliableInboxContextFactoryTests.cs`.
These are syntax-only pairing suggestions. Actual existing coverage and runtime
integration tests reach these internal types through DI; unpaired does not mean
unexecuted. New behavior tests extend the existing integration suite rather than
turn the driver assembly into a second test suite.

Product context/factory, integration test file, existing consumer-commit tests and
the changed InternalAccess driver have been fully read. Store mutation excerpts
were inspected; no new full-read accounting claim is needed.

## Implemented behavior cases

| Requirement | Exact method in ReliableInMemoryIntegrationTests | Cases |
| --- | --- | ---: |
| A failing consumer cannot recreate a removed inbox or corrupt a new lease owner's state or buffered output | ConsumerFailure_AfterRemovalOrTakeoverPreservesOwnerAndBufferedMessagesAsync | 4 |
| Real consumer commit retains a delayed envelope until its exact due boundary | ConsumerCommit_DelayedEnvelopeRemainsRetainedUntilExactDueBoundaryAsync | 1 |
| Admission failure discards an earlier buffered send; explicit operator retry commits corrected content with the same outgoing identity | ConsumerAdmission_RejectionDiscardsBufferedSendsAndOperatorRetryCommitsAsync | 1 |

Ownership cases use public store Acquire and optional quarantine/discard inside
the controlled consumer operation. MaxAttempts1/3 selects terminal/retry failure
handling. Removed entry preserves the exact original failure and can be acquired
fresh at attempt1. Taken-over entry propagates the existing ownership exception;
the new lease remains Busy at attempt2 and can complete successfully. A consumed
neighbor remains consumed at attempt1. Buffered publication has no observed
delivery and leaves no outbox records after harness shutdown.

The delayed case uses an injected fixed UTC clock with real base timers, avoiding
background claims at the future due time. The InternalAccess driver optionally
awaits actual SetConsumedAsync after a successful callback. AlreadyConsumed plus
retained outbox count proves commit. Empty pre-boundary claim plus unchanged
snapshot avoids false success from disappearance. Exact-boundary claim checks
IDs, contract, due time and structured message body before explicit delivery
completion and empty storage. It does not claim actual broker delivery.

Both parts received bounded read-only review with no concrete static blocker.
The first ownership build found a nullable lease argument in test code, manually
fixed with a typed local. Combined corrected test run
`artifacts/t45-ownership-delay.log` exits0: 14 passed, no failures or skips,
including all five additional cases (session75010 terminal).

Admission case adds one allowed send before an oversized send, with explicit
256-byte body limit. Terminal failure must quarantine the inbox at attempt1,
record the admission failure type and leave zero retained sends. A handshake
allows assertions before public requeue. The same consumer context reenters
the real factory; SetConsumedAsync commits one corrected message with the same
outgoing ID, and inbox attempt2 becomes consumed. This is controlled pipeline
reentry, not automatic transport redelivery. Bounded read-only review found no
static blocker. Initial build identified omitted cancellation token on the
cleanup TaskCompletionSource.TrySetCanceled; manually corrected. Corrected run
`artifacts/t45-admission-corrected.log` exits0: 15 passed, no failures or skips,
including all six new cases. No product source change was required.

## Remaining packet work

MAIN full Core project: `artifacts/t45-full.log`, exit0, 6,737 passed,
no failures/skips. Verify-only whitespace check `artifacts/t45-format.log` exits0.

First isolated counterprobe in GATE: replace the factory's terminal-failure
rethrow after unsuccessful quarantine with return. `artifacts/t45-mutant-missing.log`
exits2: one removed/maxAttempts1 case fails, fourteen controls pass. Restored
manually before the next independent counterprobe.

An initial lease mutation accidentally targeted the similarly written outbox
EnsureOwned helper rather than EnsureInboxOwned. Its log
`artifacts/t45-mutant-lease.log` passes15/15; it is not evidence about inbox lease
fencing. The outbox edit was restored and the intended inbox mutation applied
with an explicit method-context patch. Keep this unsuccessful probe distinct.

- Correct inbox lease guard removal: GATE `artifacts/t45-mutant-inbox-lease.log`,
  exit2, both stale-owner cases fail; thirteen controls pass. Manually restored.
- Ignore outgoing delay (`DueAt=null`): GATE `artifacts/t45-mutant-delay.log`,
  exit2, delayed commit and admission recovery cases fail; thirteen controls pass.
  Manually restored before the quarantine-state mutation.
- Pretend quarantine succeeded without storing it: GATE
  `artifacts/t45-mutant-quarantine.log`, exit2, admission recovery, both terminal
  ownership cases and existing repeated-failure quarantine case fail; eleven
  controls pass.

All mutations manually restored. The three product files, integration tests,
InternalAccess driver and requirement manifest compare byte-identically between
MAIN and GATE before the restored control. Full restored GATE control
`artifacts/t45-restored.log` exits2: 6,736 pass and the new admission recovery
case fails its global quarantine-empty assertion. This is not a passing gate.

Read-only review and source inspection trace the race to the nested test setup:
ConsumeSendEndpoint registers the rejected publish task; ConsumeContextProxy
delegates that task and ConsumeCompleted to the enclosing consumer. After the
probe signals inner completion, the outer OutboxMessagePipe awaits the failed
task and quarantines its own consumer key. The inner key remains consumed at
attempt2. The test now checks absence of that exact recovered key from quarantine,
then, after harness shutdown, asserts exactly one outer entry with the original
message ID, a different consumer ID, attempt1 and PayloadAdmissionException.
Existing recovery, envelope, capacity and no-delivery assertions remain intact.
This corrects an invalid global expectation and synchronization gap, not a
product defect. Focused restored verification
`artifacts/t45-restored-corrected.log` exits0: 15 passed, no failures or skips.
Bounded read-only review of the correction finds no blocker. The outer consumer
identity is distinguished from the inner key, not matched to its deterministic
hash. No additional full Core run is planned before the single final full33
measurement.

- The coherent packet now covers all three
  additional contracts selected by planning review (six cases). Optional token
  precedence/waiting are not duplicated: existing active-cancellation and
  concurrent-delivery cases already cover normal public paths. Artificial seams
  solely to force internal ternary/defensive branches are not introduced.
- Passing narrow run, assertion review, bounded final red-team review and
  isolated counterprobes (swallow failed transition, remove lease fence, ignore
  delayed due time, bypass admission if justified).
- Restored controls, project gates, manifest/format checks, changelog/canonical
  CHANGELIST, commit, ONE full33 measurement, independent audit, docs and push.

## Limits

Ownership uses an explicit future store timestamp to force logical takeover; no
real timer or clock-advance contract is asserted. It checks externally observable
state and ability to complete, not all private inbox fields. Existing stale-owner
behavior replaces the original consumer error with an ownership exception; this
is recorded, not silently presented as original-error preservation. Source
product code remains unchanged. The overall A+ goal remains open.

## Final measurement

Commit5c3e6c3a4 passes all33profiles,12,953executions and four fixture groups.
No measurement retry was required. Canonical aggregation confirms85,536/93,749
lines, conservative30,862/36,843branches and0CRAP>30. Detailed accounting and
remaining gaps: [measurement report](product-wide-profile-5c3e6c3a4.md).
Independent final accounting review and documentation push complete this packet.
