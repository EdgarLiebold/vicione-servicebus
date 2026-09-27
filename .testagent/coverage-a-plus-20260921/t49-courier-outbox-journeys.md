# T49 — Courier outbox, retry, revision and timeout journeys

Status: combined implementation, read-only assertion review, detected
counterprobes, SHA restoration and 69/69 restored controls complete. Final
measurement and numerical audit remain open.
Baseline is pushed T48 `7041633644cf4263d008f487cc88fcbd608416af`.
The preceding goal turn made progress by completing and remotely verifying T48.
No T49 coverage improvement is claimed yet.

## Requirements and scope

- "schneide größere pakete": combine the three connected journeys below in one
  implementation/review packet, with focused tests during development and one
  final exact-commit full33 measurement. Do not measure after each test file.
- "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und
  Regressionen hart prüfen": use real in-memory routing slips and received
  messages, gated pending activity work, exact attempts/logs/variables/owner IDs,
  positive controls and drained final counts. No overload-count padding.
- Microsoft code-testing-agent, find-untested-sources, test-gap-analysis and
  test-analysis-extensions/.NET applied before implementation. Apply run-tests,
  assertion-quality and coverage-analysis for their subsequent gates.
- Independent read-only review, isolated counterprobes, byte restoration,
  final measurement/audit, changelog, canonical CHANGELIST and authorized push
  remain mandatory. Overall A+ is open; first source reading is already complete.

Primary sources are the execute/compensate outbox and timeout contexts,
InMemoryOutboxActivityContextProxy, InMemoryOutboxFilter, TimeoutFilter and
RetryExecuteContext/RetryCompensateContext. Follow the public configuration
observers into Courier execution/compensation hosts and result evaluation.
Do not instantiate internal wrappers merely to obtain coverage.

## Existing evidence and single static pairing

The bounded Roslyn run copied 9 source files, 67 existing test files and their
two project files byte-identically into `/private/tmp/servicebus-t49-pairing`.
Inputs and output: `artifacts/t49-pairing-inputs.json`, `t49-pairing.json`,
`t49-pairing.log`. This is a static namespace-aware pairing heuristic, not
runtime coverage or evidence that a behavior is untested.

- InMemoryOutboxFilter pairs with Middleware/InMemoryOutbox/InMemoryOutboxFilterTests.cs.
- TimeoutFilter pairs with Middleware/Timeout/TimeoutConfigurationSurfaceTests.cs
  and TimeoutFilterTests.cs.
- Seven contexts are unpaired. Preserve the analyzer's suggested paths under
  `tests/ViciOne.ServiceBus.Tests/`: RetryPolicies/Contexts/RetryExecuteContextTests.cs,
  RetryCompensateContextTests.cs; Middleware/InMemoryOutbox/
  InMemoryOutboxActivityContextProxyTests.cs, InMemoryOutboxExecuteContextTests.cs,
  InMemoryOutboxCompensateContextTests.cs; Middleware/Timeout/
  TimeoutExecuteContextTests.cs and TimeoutCompensateContextTests.cs.
  The combined integration tests belong in the existing Courier test area.

Existing RoutingSlipRetryIntegrationTests proves attempt/log preservation;
RoutingSlipRevisionAndSubscriptionTests proves revision/termination separately.
ContainerRoutingSlipOutboxRequestTests proves request completion with an outbox,
but does not prove failed-attempt discard. TimeoutCancellationIntegrationTests
uses consumers; TimeoutActivityContextTests proves local notification behavior.
RoutingSlipHostConfigurationTests demonstrates public activity pipeline hooks.

## Acceptance map

| Journey / planned test | Required discriminators |
| --- | --- |
| ExecuteRetry_RevisesTheRouteAndDeliversOnlyCommittedAttemptEffectsAsync | First attempt buffers an identified effect and fails. A gated second attempt buffers a different effect; no send is admitted while blocked. On release, only the second effect is received; exact retry attempts, one revision, replacement activity and variables are observed, discarded original activity never executes. |
| CompensationRetry_DiscardsFailedEffectsAndPreservesLogAndVariableRemovalAsync | Successful activity is followed by terminal failure. First compensation buffers and fails; second sees the identical original log and removes a variable. Only successful compensation effect is delivered; exact compensated/faulted owner, log, attempt counts and final removed variable are asserted. |
| ActivityTimeout_DiscardsBufferedEffectsAndKeepsAnIndependentSlipOperationalAsync | Execute and compensate cases buffer effects and wait on their activity token. Fake time at one tick before deadline keeps work active; exact deadline cancels. No effect or success continuation escapes. Assert actual terminal failure classification and owner, with a healthy slip completing on the same bus. |

The first journey covers revision versus termination and sequential/concurrent
flush (four cases). Compensation covers successful versus exhausted retry and
both delivery modes (four cases). Timeout covers execute versus compensate and
both delivery modes (four cases). Exhausted compensation preserves the log and
reservation, emits exactly the two domain failure events, and emits no transport
fault, successful compensation or buffered effects. Timeout must not emit either
domain compensation-failure event alongside its transport fault.

Parameterize meaningful delivery modes (sequential/concurrent outbox flush)
and outcome boundaries where assertions discriminate behavior; avoid a Cartesian
matrix of equivalent API overloads. Timeout classification must follow the real
host/result pipeline, not an assumed consumer-only exception shape.

Read-only reviewer `/root/outbox_proof_redteam` independently proposed these
three journeys and identified the same existing-evidence gaps. These are static
planning observations, not empirically confirmed surviving mutations or defects.

## Implementation and focused results

New files: Courier/CourierOutboxJourneyIntegrationTests.cs and
Courier/CourierTimeoutOutboxJourneyIntegrationTests.cs under the Core test project.
Three requirement variants bind the concrete tests above in CoreRequirements.json.
No production code or project dependencies changed.

- Initial combined build: zero warnings/errors; initial ten cases passed.
  Its projection filter was misspelled, so that run proves only those ten cases.
- Expanded/reviewed build: zero warnings/errors; twelve cases plus the correctly
  named RequirementCoverageProjectionTests passed (13/13).
- Final baseline after additional negative event assertions: zero warnings/errors,
  13/13 pass, no skips (`/private/tmp/t49-baseline-{build,tests}.log`).
- Independent read-only review found one missing negative event oracle, now
  corrected, and no remaining concrete blocker. Its optional transport-fault
  exclusion for compensation was also added.
- Assertion-quality review: all three test methods have meaningful exact-value,
  collection, structural, side-effect and negative assertions. None is
  assertion-free, trivial-only or a self-comparison. Pending transport admission
  is measured at PreSend after awaited enqueue, not inferred only from a racing
  receiver snapshot. Final received counts are checked after bus drain. The
  timeout cases add exact token boundary and nested fault classification.

## Counterprobe checkpoint

Green baseline established. Omitting the compensation-outbox observer
specification in GATE was detected: four compensation cases fail at the empty
PreSend-admission assertion, while all four execute controls pass. Build had zero
warnings/errors. The source is restored and its original SHA-256 confirmed.

Using the original activity token instead of the passed timeout token is also
detected: all four timeout cases fail at the exact deadline cancellation
assertion, while eight outbox/retry controls pass. Build had zero warnings/errors.
Both product files are restored to the original SHA-256 values; MAIN sources
remain untouched. No counterprobe remains applied.

The restored build completed with zero warnings/errors. Combined controls pass
69/69, zero failures/skips. Verify-only whitespace formatting succeeds, with the
Core project loaded and zero of 5,727 files changed. All 5,877 MAIN/GATE source
and test paths are byte-identical. No build or counterprobe remains active.
Original hashes are in `artifacts/t49-source-restore-hashes.json`.
Next: commit/freeze and run the sole full33 measurement. Measurement helpers are
prepared but `t49-measure-all.py` still has a pending implementation-commit marker.
Read-only helper review confirms T48 baseline, Core +12 (13,032 total expected
executions), unchanged 33-profile/provider/CPU scope and nine primary sources.

## Remaining gates

Implement all journeys and requirement bindings; narrow build/test corrections;
combined adversarial assertion review; targeted one-cause counterprobes with
source hashes/restoration and positive controls; implementation commit; one
full33 run; physical line/branch and method-CRAP reconciliation; independent
integrity audit; completion records, canonical CHANGELIST, commit and push.
