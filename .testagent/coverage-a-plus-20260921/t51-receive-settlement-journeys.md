# T51 — receive ownership, renewal and terminal settlement

Status: implementation, three counterprobes, restored controls, read-only closure
review, verify-only formatting, frozen full33 measurement and independent audit
complete. See [full measurement](product-wide-profile-b42dcf790.md).
Baseline: completed/pushed T50 `0f7f19e30`, measured implementation `a74627818`.

## Scope and user requirements

| User requirement | Planned evidence |
| --- | --- |
| "schneide größere pakete" | One connected receive/settlement package across Core, SQS, SQL and Azure Service Bus; implement the behavior families together before combined review/measurement. |
| "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen" | Exact provider effects, delivery/exception identity, timing boundaries, retained headers, no duplicate terminal effect, and independent successor progress. |
| "die Microsoft Testskillss sind verpflichtend" | code-testing-agent Research → Plan → Implement inline, one bounded find-untested-sources Roslyn pass, test-gap-analysis plus .NET extension, assertion-quality review and run-tests; coverage-analysis for final measurement. |
| "wie immer mit adversal red team reviews" | Existing independent read-only reviewer checks scope and completed tests; isolated counterprobes with immediate manual restoration and final controls. |

No broad measurement is needed for selection. T50b JSON inventories are the
authoritative measured baseline. This package does not declare a numerical A+
threshold or substitute zero CRAP > 30 for global acceptance.

## Bounded research

The receive lifecycle includes lock validation, dispatch, asynchronous receive
completion, provider settlement and duplicate-delivery fallback. Provider-specific
semantics remain separate: SQS visibility/deletion, SQL lock transitions and
headers, Azure lock/TTL validity and abandonment.

One Roslyn parse-only run used a byte-identical bounded mirror at
`/private/tmp/servicebus-t51-pairing`: 35 source files, 16 test files and 13
project files. It reports 20 paired and 15 unpaired sources. Inputs and exact
suggested paths are retained in `artifacts/t51-pairing-inputs.json` and
`artifacts/t51-pairing.json`. This is a static pairing heuristic, not evidence
that an unpaired source lacks execution coverage. For example, the bounded
filename selection does not include ServiceBusTimeProviderTests; those tests
are inspected separately before adding Azure cases. No repeated pairing scan.

Existing paired evidence:

- AmazonSqsReceiveLifecycleTests already checks monotonic renewal clipping,
  maximum duration expiry, unrequested cancellation, snapshotted settings,
  failed completion and caller cancellation linked to settlement.
- SqlReceiveLockContextTests already checks rejected redelivery, callback
  rejection, concurrent terminal operations and cancellation of active renewal.
- ReceiveLockContextTests already checks no-lock behavior, valid duplicate
  fallback, single concurrent completion and cancellation of retained contexts.
- ReceivePipeDispatcherTests already checks fault observer isolation, combined
  pipeline/settlement failures and zero-activity subscriber isolation.
- RabbitMQ settlement already has directly paired tests; inspect before adding
  any case, and do not expand merely to increase the provider count.

## Acceptance map (planned test names)

| Family | Planned test | Distinguishing contract |
| --- | --- | --- |
| SQS terminal renewal failures | `RenewalFailure_StopsRenewalAndRetainsFaultSettlementIdentityAsync` | Message-not-inflight, invalid receipt and provider failure end renewal; lock validation fails; fault redelivery uses the original receipt, queue and snapshotted delay, never deletion. |
| SQS cancellation before settlement | `CanceledSettlement_LeavesTheActiveDeliveryAvailableForAnotherCallerAsync` | Pre-canceled complete/fault performs no provider settlement and does not consume the active lock; a subsequent legitimate caller can settle it. |
| SQS renewal drain | `Settlement_DrainsCanceledRenewalBeforeAnyTerminalProviderEffectAsync` | Complete/fault cancels in-flight renewal, waits for its controlled exit and only then deletes or adjusts redelivery visibility. |
| SQL terminal effects | `TerminalSettlement_PreservesExactDeliveryHeadersAndReportsRejectedOwnershipAsync` | Complete/fault/redelivery/expiry target the exact lock/delivery; rejection is reported; fault/redelivery/expiry metadata and delay are exact, with no second terminal effect. |
| SQL renewal outcomes | `RenewalOutcome_ControlsLockValidityAndStopsFurtherProviderWorkAsync` | Timeout retry versus rejected renewal/provider failure differ in lock validity and subsequent effects; deterministic time/gates avoid wall-clock sleeps. |
| Duplicate fallback | `FailedFallbacks_PreserveFinalFailureAndAllowANewDeliveryAsync` | Ordered failing complete/fault attempts preserve final base exception and clear retained state; a new delivery can be admitted and settled. |
| Queued cancellation | `CanceledWaiter_DoesNotConsumeAnotherDeliverySettlementAsync` | A canceled waiting operation performs no provider action while an admitted operation is gated, and does not corrupt eventual completion. |
| Dispatch ordering | `DispatchCompletion_OwnsSettlementAndFailureNotificationUntilReceiveWorkEndsAsync` | Delayed ReceiveCompleted gates completion; failure before/after dispatch gives exact ordered observer/provider effects and zero-active metrics; a healthy successor remains operational. |
| Azure expiry/abandonment | `FaultSettlement_AbandonsOnlyRecoverableFailuresWithOriginalIdentityAsync` | Actual validation failures feed settlement; terminal versus retryable provider reasons preserve original failure/token and exclude unintended completion. |
| Azure asynchronous abandonment | `FaultSettlement_AwaitsAbandonAndContainsProviderFailureAsync` | Provider completion remains awaited; provider error is contained according to the best-effort abandonment contract, with no extra completion/dead-letter effect. |

These are acceptance names and static hypotheses, not claims of empirically
confirmed surviving mutations. Compare remaining Azure tests and source contracts
before implementing; remove duplicate cases or refine this map explicitly.

## Initial read-only selection review and implementation checkpoint

Reviewer `/root/outbox_proof_redteam` confirmed the connected scope and concrete
missing combinations as static hypotheses. Azure exact clock boundaries are
already covered by ServiceBusTimeProviderTests and are not duplicated as separate
tests. New coverage connects actual validation failures to settlement policy.
SQS must distinguish renewal calls from intentional fault-redelivery calls;
SQL terminal rejection must not imply retryability of the same lock object;
pending fallback must preserve the final base exception rather than its wrapper.

The first new test file follows the retained Roslyn suggested path:
`tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests/AzureServiceBusTransport/ServiceBusReceiveLockContextTests.cs`.
It implements eight settlement classification cases and two controlled asynchronous
abandonment cases. These ten cases are not yet built or executed; requirement
attributes/manifest bindings remain to be added with the combined packet.
Core duplicate settlement now adds six cases in
`PendingReceiveSettlementBoundaryTests`: validate/complete/fault exhaust three
FIFO fallbacks, preserve the final base exception and accept a healthy successor;
three canceled waiting operations leave an admitted completion untouched.

SQS now adds seven cases in `AmazonSqsSettlementBoundaryTests`: three renewal
failure classifications, two pre-canceled settlement cases and two controlled
renewal-drain paths. Exact queue/receipt/snapshotted redelivery delay and absence
of duplicate provider effects are asserted. No fixed sleeps were added.

SQL terminal settlement adds eight accepted/rejected cases in
`SqlSettlementBoundaryTests`: delete, fault-unlock, redelivery-unlock and expiry
move verify exact lock/delivery/token, delay, queue kind, expiration and headers.
The terminal object cannot issue a second provider effect, even after rejection.

ReceivePipeDispatcherTests adds two controlled ReceiveCompleted cases: processing
completion gates settlement, observer order and zero activity; success/failure
retain exact outcomes and a healthy successor uses the same dispatcher. Its
existing TestReceiveContext now optionally takes the pending completion task;
existing tests retain their completed-task default.

The paragraphs above record the initial implementation checkpoint. The current
total is ten new methods and 39 cases across five changed/new test files, all
requirement-bound and copied byte-identically to GATE. SQL renewal adds three
outcomes: timeout then successful retry, rejected renewal and provider failure.
Core fallback now explicitly distinguishes normal exception wrappers from
AggregateException wrappers, across validate/complete/fault (six cases).

## Focused validation and review

Initial validation logs are retained in `/private/tmp/t51-initial-validation`.
Core initially failed three incorrect AggregateException expectations; its
GetBaseException stops at the first non-aggregate wrapper. The corrected test
asserts exact object identity for both wrapper families. Four SQS xUnit1051
diagnostics were corrected with explicit test cancellation tokens. SQL restore
initially rejected its changed test dependency in locked mode (NU1004); normal
force-evaluate restore added only Microsoft.Extensions.TimeProvider.Testing
10.9.0 to that test project's lockfile, copied byte-identically to MAIN.
These were authoring/setup corrections, not product defects.

Corrected controls in `/private/tmp/t51-corrected-validation` pass Core21/21,
SQS17/17 and SQL16/16. Initial Azure controls pass18/18; its later cleanup-token
adjustment requires the final combined rerun. Cleanup releases controlled work
in finally and awaits it using CancellationToken.None and the configured timeout.

Read-only reviewer `/root/outbox_proof_redteam` found no concrete false-green
blocker in the completed test families. Scope limits remain explicit: dispatcher
tests gate ReceiveCompleted but do not separately gate fault observers; Azure
tests prove abandonment failure containment, not warning-log payload contents.

## Isolated counterprobes

Logs: `/private/tmp/t51-counterprobes`. Changes are manual, GATE-only and restored
immediately after each targeted run. No intentional product change is retained.

- Remove ReceiveCompleted awaiting: build succeeds; dispatcher5 cases yield
  exactly2 failures (both new controlled cases),3 passes,0 skips. Both failures
  identify premature dispatch completion. Manual restoration SHA-256:
  `7cf39894fe7d647da231c19a3a433b648b6fd63c4a2767fe10a48b25b340a9a7`.
- Remove SQS visibility-task awaiting: build succeeds; seven cases yield exactly
  two failures (both drain cases), five passes and no skips. Both failures identify
  premature settlement. Restored SHA-256:
  `9aed84f4b739cb3d08a4173a8399da74ae28b91c1e309642b328064c000d95d7`.
- Ignore SQL MoveMessage refusal: build succeeds; eleven cases yield exactly one
  failure (Expire/refused), ten passes and no skips. The oracle detects the absent
  TransportException. Restored SHA-256:
  `bdc2fa58543bc33b85b15c174db84df288e83d7108b2036a4313b97ea010a717`.

Final restored controls pass Core21, SQS17, SQL16 and Azure18: 72 total, no failures
or skips. All four builds exit0. Logs are also retained under repository
`artifacts/t51-final-controls` and `artifacts/t51-counterprobes`, alongside initial
and corrected validation logs. MAIN/GATE byte comparison covers all 5,884 tracked
and new nonignored src/tests paths, with zero mismatches. No mutation remains.

## Assertion-quality assessment

Microsoft assertion-quality and its .NET extension were applied to the ten new
methods. Static assertion-call counts include assertions inside callbacks and
conditional branches; they are not runtime counts or an A+ scoring formula.

| Method family | Assertion calls | Behavioral facets |
| --- | ---: | --- |
| Azure failure classification | 11 | Exception and message identity, exact token, abandonment policy, forbidden completion/dead-letter effects |
| Azure asynchronous abandonment | 9 | Pending state, exact original failure, single awaited effect, successful containment |
| Core exhausted fallback | 16 | FIFO collection, exact final exception, token and failure forwarding, empty state and healthy successor |
| Core canceled waiter | 11 | Cancellation identity/status, retained owner, no extra effect, final empty state |
| SQS failed renewal | 11 | Lock-loss error, exact queue/receipt/delay, no renewal restart or deletion |
| SQS canceled settlement | 7 | Cancellation token, absent effects, subsequent legitimate deletion, final invalidity |
| SQS renewal drain | 10 | Provider arguments, incomplete settlement, ordered effects, final invalidity |
| SQL renewal outcome | 10 | Delivery identity, retry count, exact duration, conditional deletion, lock validity |
| SQL terminal settlement | 36 | Provider call and typed arguments, preserved/changed headers, rejection, no second effect |
| Dispatcher receive completion | 21 | Pending state, ordered notifications, exact failure, metrics and healthy successor |

Total142 calls, average14.2 per method. No assertion-free, trivial-only or
self-comparison method; all ten check state/provider effects and forbidden effects.
Nine methods contain explicit exception assertions; Azure asynchronous abandonment
instead asserts successful task status because containment is its contract.
Counts do not substitute for the three empirical counterprobes. Existing limits
on separately gated fault observers and Azure log payloads remain unchanged.

Closure reviewer confirms no remaining concrete blocker, all72 final controls and
all three restored product files. Local provider-boundary tests do not themselves
prove broker integration; the full33 run must retain its broker fixtures.
Verify-only formatting of the five test files through the Unit solution exits0,
changes0/5,734 workspace files; the known analyzer metadata load warning remains
in the log. Strict focused builds independently succeed. Canonical CHANGELIST is
regenerated with process-local exclusions for protected TestResults/ and review/.

## Execution plan

1. Finish bounded existing-test comparison, then implement all families manually.
2. Bind new variants in the existing requirement manifests; build the affected
   projects and run focused new/existing controls as one implementation checkpoint.
3. Resolve concrete failures, read-only adversarial review and assertion-quality
   audit. Run only counterprobes that distinguish material product behavior.
4. Restore every probe immediately, verify source hashes, run combined controls
   and verify-only formatting, freeze implementation and run one complete full33.
5. Independently audit totals/deltas, update changelog/canonical CHANGELIST and
   commit/push. No global A+ or deferred API-audit completion is implied.
