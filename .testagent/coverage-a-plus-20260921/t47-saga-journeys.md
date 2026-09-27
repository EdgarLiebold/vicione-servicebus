# T47 — Saga request and callback journeys

Status: implementation in progress. Baseline: `1011ef1dd3fe3115c28e6ca0d05ca25b8d515dc8`.
This is one combined packet, with one final full 33-profile measurement after
implementation, adversarial review and restored counterprobes. No A+ closure is claimed.

## Research

The Microsoft code-testing-agent workflow is applied inline. The existing
first-read completion remains accepted. The one bounded Roslyn pairing run
(`artifacts/t47-pairing.json`, exit 0) covered three product sources and 88 tests.
ViciOneServiceBusStateMachine paired with three existing test files.
StateMachineRequestExtensions and SendCallbackExtensions were classified unpaired;
the suggested paths were respectively
`tests/ViciOne.ServiceBus.Tests/Sagas/StateMachineRequestExtensionsTests.cs` and
`tests/ViciOne.ServiceBus.Tests/Sagas/SendCallbackExtensionsTests.cs`.
These extension-class naming results do not mean runtime behavior is untested.
New integration tests belong with the existing SagaStateMachine journey tests.

Existing evidence avoids duplicate matrices:

- StateMachineMessageFactoriesDeepContractTests covers 58 factory forms, pending
  tasks, callback/pipe ordering, exceptions and cancellation identity.
- StateMachinePublishSendRespondActivitiesDeepContractTests records endpoint
  calls and pipes, but its endpoint does not execute those pipes.
- StateMachineTransportIntegrationTests executes real sends, without the combined
  saga/event-dependent callback metadata contract.
- StateMachineRequestIntegrationTests covers parallel requests and the second
  and third accepted responses with real transport.
- QuartzSagaIdRequestIntegrationTests covers first/second/third responses, faults,
  real timeouts, custom body ownership versus header ownership, and missing request
  headers, with an independent control saga and scheduler-state assertions.
- QuartzSagaRequestTimeoutIntegrationTests covers delivered expiration and actual
  cancellation of the trigger with clearing of the dedicated request identity.

Read-only planning review identifies real callback execution as the missing seam.
It explicitly rejects another overload-count matrix and warns that waiting in a
normal SendActivity factory is not itself a token-cancellation guarantee.

## Acceptance and implementation plan

| Contract | Planned evidence |
| --- | --- |
| Saga and event determine body, destination and callback metadata without crossing instances | `CallbackJourney_PreservesOwnershipAndCompletesOnlyTheSelectedOutcomeAsync`, immediate and deferred success, two differently primed instances |
| Pending factory does not invoke callback, deliver or execute successful continuation before release | Same test, deferred cases, controlled gate plus independently completed control saga and ordered activity trace |
| Factory failure and callback failure prevent normal delivery and successful continuation | Same test, two distinct failure stages, exact caught exception and drained normal-send counts |
| Fault callback sees original event, owning saga and actual exception and delivers recovery metadata | Same failure cases, real recovery endpoint and exact payload/header/state assertions |
| Actual pending send cannot advance the Saga; a send-filter failure reaches compensation without normal delivery | Same callback test, DeferredSend and SendFailure |
| Failure inside the recovery callback must not count as successful compensation | Same callback test, RecoveryCallbackFailure; exact local exception identity and published Fault payload/type/message |
| Late messages from Request1 cannot affect Request2 of the same Saga | `PreviousRequestMessages_CannotCompleteOrCancelTheNextRequestOfTheSameSagaAsync`, response/fault/timeout cases, exact receive-completion barrier, request identity/state/counts, actual Quartz trigger and observed cancel tokens; valid Request2 completion as positive control |
| Request responses, missing IDs and timeout cleanup retain correct ownership | Existing request and Quartz journey suites in the combined narrow verification, then full provider measurement; extend only if review identifies a concrete missing behavior |
| Larger coherent packet, strong tests and independent adversarial review | Review all new cases together, isolated single-cause counterprobes, restore and verify before one final full33 |

Pending: final exact-commit measurement and numerical
audit, changelog/CHANGELIST, commit and authorized push. Implementation, bindings,
read-only review, selected counterprobes and restored focused checks are complete.

## Implementation checkpoint

- Seven callback cases and requirement projection pass 8/8 in GATE, Release build
  zero warnings/errors (`artifacts/t47-callback-recovery-corrected*.log`).
- Two rejected attempts remain recorded: initial compiler rejected the noncanonical
  Assert.Single(Where(...)) form; expanded run passed 7/8 because the new test
  incorrectly expected EventExecutionException around the recovery callback error.
  Source review of CatchFaultActivity/ActivityBehavior confirms direct propagation;
  exact direct type/message and local exception identity now pass. Product unchanged.
- Read-only adversarial callback review finds no concrete blocker; counterprobes
  are still required. No empirical mutation verdict is claimed yet.
- Generation tests extend the original plan following review: dedicated request-ID
  correlation is the isolation boundary; mistaken Saga-ID correlation could invoke
  CancelRequestTimeout on the next request. Existing Quartz tests did not exercise
  two successive request generations of the same Saga.
- The three new generation cases and requirement projection pass 4/4 in GATE,
  Release zero warnings/errors (`artifacts/t47-generation-corrected*.log`).
  Initial compile failed on missing Registration/Middleware imports and use of
  ReceiveContext without the explicit Advanced view; these fixture issues were
  corrected manually. The PostReceive
  barrier includes address and unique envelope ID, with receive faults surfaced.
  Sent cancel-token snapshots catch cancellation already enqueued but not consumed.
- No full33 measurement has been launched for T47.
- Final read-only generation review requested positive proof that the first
  Quartz trigger exists before its response cancels it. That assertion was added
  and is included in the green run. Both actual triggers are now observed present
  before their respective removal; this is stronger than schedule-command capture.
- Assertion review: exact independent payload/header literals, two distinct owners,
  local exception identity plus serialized error type/message, ordered activity
  traces, success/recovery counters and terminal message multiplicity discriminate
  behavior. No assertion-free or trivial-only case. Scope remains in-memory Saga
  storage and transport with real Quartz, not external broker or durable Saga proof.
- Isolated counterprobes completed:
  - Removing only the typed SendActivity callback pipe compiles cleanly, then all
    seven callback cases fail the delivered CorrelationId assertion (null instead
    of the independent wire ID). `t47-counterprobe-callback-pipe.log`, exit 2.
  - Replacing only the dedicated-ID timeout correlation with Saga-ID correlation
    compiles cleanly; timeout replay changes Request2 to ExpiredState and fails
    its pending-state assertion. Response/fault controls pass, 1/3 failures.
    `t47-counterprobe-stale-timeout.log`, exit 2.
  - Both changes were manually reversed. SHA-256 of SendActivity is
    `5211ee156a418b60edf48cf8a4eb660bc37b26ba9b121f5cd90c1e0d7ef0f5f0`;
    ViciOneServiceBusStateMachine is
    `b34154afbdcca7bef514d35fd589519f2ae14c7b9d2bdb1f940cb87f5856eeae`.
    Both match MAIN; no mutation remains.
- Restored combined focused verification passes Core 20/20 and Quartz 22/22,
  each with zero build warnings/errors. This includes existing parallel and
  multi-response requests, nested requests, Saga-ID ownership, missing headers
  and real timeout delivery/cancellation, plus both requirement projections.
  Logs: `t47-core-restored*.log` and `t47-quartz-restored*.log` in GATE artifacts.
- The read-only reviewer confirmed the first-trigger oracle correction and found
  no remaining implementation blocker. No durable Saga/external-broker claim and
  no untested automatic factory-cancellation guarantee is inferred.
- Product source in MAIN remains unchanged. No global A+ claim is made.
- All 5,873 current source/test paths compare byte-identically between MAIN and
  GATE after mutation restoration. `artifacts/t47-counterprobe-evidence.json`
  binds twelve build/test logs and both restored product source hashes.
- The first verify-only formatting command used the product solution, which did
  not load either test project. Its exit 0 is rejected as evidence for the new
  files; the correction uses `ViciOne.ServiceBus.Tests.Unit.slnx`.
- Corrected verify-only formatting exits 0 with both target test projects loaded
  and no formatting changes required (`t47-format-tests.log`). `git diff --check`
  passes. Canonical CHANGELIST generation and verification pass, 16,535 entries.
