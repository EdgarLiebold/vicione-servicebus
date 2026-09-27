# T55 — consumer-outbox retention and recovery

Status: implementation in progress. Baseline T54 is complete and remotely verified
at `5acd82701`, measured at `0fa2c85bb`. Three focused boundary cases fail for the
predicted causes on unchanged product source; two product corrections pass focused
controls. Real persisted-row retention/recovery now passes33 cases, including
pending-send deadlines and late failures. Combined counterprobes are detected and
restored; frozen full-product measurement and complete packet acceptance remain.

## Requirements and packet boundary

- User: "schneide größere pakete". Complete the connected families below before
  one combined review, counterprobe cycle and exact full-product measurement.
- User: "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen
  und Regressionen hart prüfen". Verify retained persisted rows, committed
  delivery positions, consumer invocation counts, exact outgoing identities and
  recovery; no tests whose only oracle is executing an uncovered branch.
- Microsoft code-testing-agent Research → Plan → Implement applies inline because
  the named generator is unavailable. Use test-gap-analysis and assertion-quality
  with the .NET extension, run-tests for execution and coverage-analysis for the
  eventual exact complete measurement. Source/test/comment edits remain manual.
- Keep existing source-reading acceptance and protected `TestResults/`, `review/`.
  Finish this entire packet before a new packet or another full measurement.

## Bounded static pairing and existing evidence

One Microsoft Roslyn run on118 byte-identical inputs reports78 sources,35 tests,
five project files,37 paired and41 unpaired sources. Inputs and output are retained
in `artifacts/t55-pairing-inputs.json`, `t55-pairing.json`, `t55-pairing.log` and
`/private/tmp/servicebus-t55-pairing`. This is a static heuristic, not runtime
coverage or proof of absence of indirect tests.

The analyzer suggests:

- `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests/Outbox/DbContextOutboxConsumeContextTests.cs`
- `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests/Outbox/EntityFrameworkOutboxContextFactoryTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Middleware/OutboxMessagePipeTests.cs`
- `tests/ViciOne.ServiceBus.Tests/Middleware/Outbox/InMemory/InMemoryOutboxConsumeContextTests.cs`

Those sources are unpaired by the heuristic, but existing
`InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSetAsync`
already proves public UseEntityFrameworkOutbox row locking, one consumer invocation,
16 exact effects and final row removal across three concurrent deliveries. Preserve
that evidence; do not duplicate it under a new name.

`BusOutboxReliabilityStateTests.MissingDestination_IsPersistedAsQuarantinedAndNeverDeletedAsDeliveredAsync`
proves the separate bus-outbox quarantine service. Transactional-outbox local tests
already verify exact envelopes, signals/due-time retries and distinct batches.
Neither proves corruption retention in the receive-side consumer outbox.

## Source findings requiring behavioral evidence

OutboxMessagePipe skips a loaded row whose DestinationAddress is null, may advance
the watermark past it on a subsequent successful send, and may mark the batch
delivered. DbContextOutboxConsumeContext subsequently deletes all owned rows.
Its loader reads persisted rows after the watermark and deserializes their
metadata without rejecting a missing destination. OutboxMessageFactory stores
DestinationAddress as supplied. This establishes a source-level risk, not an
executed reproduction or an accepted fix.

The newer EF and in-memory ReliableInboxContext implementations return empty
outgoing lists and report IsMessageConsumed=false; they do not prove this path.
The existing public UseEntityFrameworkOutbox integration confirms the older path
is reachable. The recovery contract must preserve at-least-once semantics:
rollback after transport success may cause duplicates; do not assert an unsupported
exactly-once transport guarantee.

## Planned combined behavior matrix

| Family | Planned named evidence | Required distinguishing observations |
| --- | --- | --- |
| Persisted corruption and repair | `CorruptPersistedMessage_PreservesRowsAndRecoversAfterRepairAsync` | Corrupt first/middle/last destination or metadata; retain exact row/envelope and consumed fence; no watermark past the corrupt row; repair and continue without invoking the consumer again. |
| Delivery windows and partial failure | `DeliveryFailure_PreservesCommittedWatermarkAcrossWindowsAsync` | Limits at and around batch boundaries; fail first/middle/last transport operation; only committed positions suppress replay, no missing effects, exact identity on allowed replay. |
| Cancellation and pending delivery | `PendingDelivery_CancellationPreservesUnacknowledgedRowsAsync` | Gate endpoint delivery, cancel owner, prove no premature acknowledgment/removal; release and observe all work; subsequent attempt recovers retained rows. |
| Endpoint resolution deadline | `EndpointResolution_ObservesDeliveryCancellationBeforeSendAsync` | Block resolution separately from sending; prove caller cancellation and delivery timeout reach it, no send/acknowledgment, then successful recovery. |
| Persistence failure and recovery | `PersistenceFailure_PreservesConsumedFenceAndRecoverableEffectsAsync` | Save/commit/removal failure with real transactions; exact original failure ownership, retained recoverable effects and no consumer rerun after committed consumption. |
| Neighbor isolation throughout recovery | Included in each persisted-state scenario | Same input message with another consumer and another input for the same consumer remain byte/state-identical. |

This table preserves planned names; the execution record below identifies the
implemented cases. Adversarial selection review
confirms the scope and adds the constraints below. Use real relational
storage for transaction claims, deterministic gates for pending operations, and
strict bounded cleanup. A controlled mutation is accepted only after its specific
behavioral assertion fails and the product source is manually restored/hash-checked.

## Accepted adversarial selection findings

Read-only reviewer `/root/outbox_proof_redteam` confirms the separate public
consumer route and the missing-destination retention risk, still without runtime
reproduction. The consumer route has no existing OutboxState quarantine promise;
do not transplant that state machine merely to satisfy a test.

The delivery timeout starts before GetSendEndpointAsync, but that call receives
no token. Test resolver cancellation separately from pending send cancellation.
Use committed first windows before failing a later window; only committed
watermarks suppress replay. A duplicate from a failed uncommitted window is
allowed, but its exact envelope must remain intact.

Inject deterministic persistence failures before actual commit, and observe the
commit barrier through TransactionCommittedAsync. A consumer/send TCS is not a
commit acknowledgment. After cleanup failure on a committed Delivered state,
retry must delete retained rows without new sends or consumer execution. Review
counterprobes include removing the ConsumerId delete filter, advancing a watermark
before send success and swallowing a delivery-save error; their effectiveness
must be established by execution, not this static reasoning.

## Completion gates

### Combined counterprobes

Both manually injected faults were confined to the isolated GATE checkout and
compiled with zero warnings/errors. Removing ConsumerId from the delete predicate
makes all three cleanup cases fail at the exact neighbor-snapshot assertion
(`artifacts/t55-neighbor-mutation.log`). Omitting delivery acknowledgment only for
the final short batch makes limits2/10 fail on the exact committed watermark; limit1
passes (`artifacts/t55-watermark-mutation.log`). These are causal assertion failures,
not hangs. Both edits were manually restored, with original SHA256 verified:
DbContextOutboxConsumeContext `dce388dfaeab5d850bd8546ed2b1c7f0e7709cec95ac13ef26c00df897895c24`;
OutboxMessagePipe `c7507d8844cfb31c4c8182809a480c34b707e44121dd872af5f0a9a91c46c403`.
Final restored control passes34/34 without skips (`artifacts/t55-restored.log`,
fixture `vicione-4110a4c9632d`); restored build has zero warnings/errors. Combined
read-only review reports no static blocker. Verify-only format passes for both
test files and the product correction. The first relational format check found
nine initializer whitespace issues, all manually corrected; no automatic source
rewriter was used. This whitespace-only edit is not a reason for another local
runtime cycle; the frozen complete measurement will rebuild it. Frozen complete
measurement and full-product acceptance remain pending.

Independent final read-only evidence audit confirms both34/34 controls, causal
3/3 and2/3 mutation failures, restored source hashes and all five MAIN/GATE
product/test/manifest input hashes. No blocker was reported. It explicitly retains
the requirement to rebuild the final whitespace-corrected tests in the frozen full
measurement and does not approve global A+ or native-provider delivery claims.

### Relational recovery implementation

`ConsumerOutboxRecoveryTests` drives the public UseEntityFrameworkOutbox route
with real PostgreSQL. The consumer commits five outgoing intents, then a
TransactionCommittedAsync gate pauses the next pass. Only after this barrier does
an independent DbContext damage the stored row and seed two neighboring keys.

- `CorruptPersistedMessage_PreservesRowsAndRecoversAfterRepairAsync`: nine cases,
  indices0/2/4 × limits1/2/10, null destination after commit, exact retained rows,
  consumed fence and committed watermark, repair and complete delivery without
  another consumer invocation.
- `DeliveryFailure_PreservesCommittedWatermarkAcrossWindowsAsync`: nine cases
  over the same matrix, an exact PreSend-observer failure and later recovery.
  This proves an awaited pre-send failure, not a late broker/provider failure.
- `PersistenceFailure_PreservesConsumedFenceAndRecoverableEffectsAsync`: six
  cases, Save/Commit × limits1/2/10, controlled failure before the real commit,
  exact exception identity and fresh database observations after terminal receive.
- `CleanupFailure_RetryRemovesOnlyOwnedRowsWithoutSendingAgainAsync`: three cases,
  limits1/2/10, failed delete after committed Delivered, retained rows and successful
  later cleanup without additional sends or another consumer invocation.

All cases protect two exact neighbors (same input/other consumer and other input/
same consumer) through full row snapshots. Delivery assertions compare payload,
MessageId, custom header and cardinality after bus stop. Already committed windows
never replay; externally sent but rolled-back-window effects may replay once in
these controlled two-attempt scenarios. This does not promise exactly-once delivery.

The initial relational authoring build required the correct context.Advanced()
endpoint API. Corrected build has zero warnings/errors. First corruption control
passes9/9 (`artifacts/t55-relational.log`). The expanded run passes23/28 but five
Save/Commit scenarios time out because the fixture listened only to ReceiveFault;
factory NotifyFaultedAsync<T> instead uses ConsumeFault. No product defect is
inferred from those fixture timeouts.

The corrected probe captures both matching fault notifications, then releases the
failure observation only at the input PostReceive callback, after pipeline and
settlement. Read-only review confirms no concrete false-green or gate deadlock.
This fixture uses the ordinary handling error pipeline: a dispatcher escape or
settlement failure may prevent PostReceive and must remain a bounded failure,
not a claimed general terminal-observer contract.

Restored expanded control passes28/28 (27 cases plus requirement projection), no
skips, build zero warnings/errors. MAIN `artifacts/t55-relational-observer.log`
and `t55-relational-observer-fixture` retain the successful run and clean PostgreSQL
fixture; GATE `artifacts/t55-relational-observer-build.log` retains the build.
No full-product measurement has been started for this incomplete packet.

### Pending send implementation (validation in progress)

`PendingDelivery_PreservesIntentUntilFailureAndRecoversAsync` adds six cases:
late asynchronous failure and delivery deadline, each with limits1/2/10. A
PreSend gate holds effect2 after actual committed consumption; a fresh PostgreSQL
context checks retained rows, the consumed fence and the prior committed window.
The same recovery oracle verifies exact effects, allowed replay and neighbors.
The failure seam is a send-pipeline observer, not native provider I/O. Disposal
releases the gate before joining bus shutdown. Build passes with zero warnings
and errors. First runtime attempt `artifacts/t55-pending.log` passes31/34;
three deadline cases time out waiting for PostReceive, which dispatch cancellation
does not invoke. This is a fixture-observation failure, not accepted evidence.

Read-only review found no gate deadlock. A second pending/token check after database
observations now rejects a deadline that elapsed during the snapshot; the new theory
is requirement-bound. Only deadline OperationCanceledException may signal directly
from the matching input ReceiveFault after the EF rollback attempt. Subsequent fresh
database reads verify its effect; the exact linked token must be canceled. This
signal precedes fault settlement, so only final bus stop proves joined work.
Ordinary failures still wait for PostReceive. Corrected build has zero warnings/
errors and runtime passes34/34 (33 PostgreSQL cases plus requirement projection),
without skips: `artifacts/t55-pending-corrected.log`, fixture run
`vicione-d26f18c0572d`. Combined counterprobes and final packet acceptance remain
outstanding; no complete measurement has started.

### First boundary implementation

`OutboxMessagePipeTests.MissingDestination_FaultsWithoutAcknowledgingOrCompletingTheOutboxAsync`
expects a specific invariant exception and no acknowledgment/completion/removal.
`EndpointResolution_ObservesDeliveryCancellationBeforeSendAsync` has caller and
delivery-deadline cases, exact observed token cancellation, no successful outbox
state transitions and bounded joined cleanup. The strict context seam does not
claim relational persistence or real broker delivery.

The first authoring build found a qualified Pipe namespace error and xUnit1051
on parameterless TrySetCanceled; both are corrected. The corrected build has
zero warnings/errors. GATE `artifacts/t55-core-red.log` exits2 with exactly3/3
failures: missing-destination returned no exception, and both resolver cases
received a noncancelable token. No unrelated exception is counted as a detection.

Manual product changes reject missing DestinationAddress before advancing the
delivery loop and pass the linked delivery token into endpoint resolution.
The corrected GATE build exits0 with zero warnings/errors; focused cases plus
requirement projection pass4/4 without skips (`t55-core-fixed.log`). The red and
green runs use the same test assertions. These focused cases are the first part
of this larger packet, not a reason to run a full-product measurement now.

Implement the combined matrix, bind requirement variants, inspect every generated
assertion and complete the read-only review. Run focused controls while building;
then isolated material counterprobes and final restored controls. Freeze the
complete packet, perform one full33/provider measurement, independently audit
integrity and numeric results, document actual product fixes and residual gaps,
update canonical CHANGELIST and push the authorized branch. No global A+ claim
until the full remaining worklist is resolved with evidence.
