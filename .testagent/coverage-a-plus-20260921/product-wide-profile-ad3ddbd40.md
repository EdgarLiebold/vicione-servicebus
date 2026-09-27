# T49 — Courier outbox, retry and timeout evidence

Status: the sole complete product-wide measurement, reconciliation and independent
numerical/integrity audit pass without discrepancies or blocking findings.
Overall A+ acceptance remains open.

## Exact measured inputs

| Input | Identity |
| --- | --- |
| Implementation commit | `ad3ddbd4050f04d8096c1054d9b509cd015ab883` |
| Product source tree | `6dc2700433374e9108821fd2a16e2f76404108a2` |
| Test tree | `84b8bc19c9d8c0910707e26444468f575ece30b9` |
| Previous measured commit | `3422dc2fd46687c68af55fba4aa08b76d48536dd` (T48) |

All 33 receipts bind the same implementation commit. Product bytes, measured
source/line denominator and method identities remain unchanged. No product code,
dependency or provider selection changed in this packet. The first source reading
remains complete under the accepted Git/manual convention.

## Combined behavior package

The Microsoft code-testing-agent workflow, one bounded find-untested-sources
Roslyn pass, test-gap-analysis, assertion-quality, test-analysis-extensions/.NET,
run-tests and coverage-analysis were applied. The bounded inventory and existing
test comparison are in [T49 research](t49-courier-outbox-journeys.md).

| Acceptance item | Exact test and discriminating evidence |
| --- | --- |
| Execute retry, buffered effects and selected route | `CourierOutboxJourneyIntegrationTests.ExecuteRetry_RevisesTheRouteAndDeliversOnlyCommittedAttemptEffectsAsync`: four cases combine revision/termination and sequential/concurrent delivery. Attempts 0 and 1 are observed with original arguments/variables. PreSend admission and receiver queues remain empty while attempt 1 is gated. Only its two exact effects arrive after release; the discarded activity never runs. Exact revised/terminated itinerary, replacement destination and final variables are asserted. |
| Compensation retry, log and terminal outcome | `CourierOutboxJourneyIntegrationTests.CompensationRetry_DiscardsFailedEffectsAndPreservesLogAndVariableRemovalAsync`: four cases combine successful/exhausted retries and both delivery modes. Original log and reservation survive both attempts. Success removes the variable and delivers only attempt 1 effects; exhaustion preserves the reservation, emits the exact activity/slip compensation failures and delivers no effects. Transport faults and incompatible successful outcomes are excluded after drain. |
| Activity deadline and independent progress | `CourierTimeoutOutboxJourneyIntegrationTests.ActivityTimeout_DiscardsBufferedEffectsAndKeepsAnIndependentSlipOperationalAsync`: four cases combine execute/compensate deadlines and both delivery modes. A healthy slip completes on the same activity endpoint while the other is gated. Fake time one tick before the deadline leaves its token active; the exact deadline cancels it. Buffered effects and successful continuation remain absent. The transport fault retains the failing slip identity and exact nested cancellation classification. Both domain compensation-failure events and incompatible terminal outcomes are excluded. |

The three variants are bound in CoreRequirements.json and verified against
compiled requirement attributes. All tests use real in-memory transport and
public configuration; no internal wrapper is constructed to inflate coverage.
PreSend observes admission after awaited enqueue, avoiding a receiver-scheduling
race as the sole negative oracle. Exact final counts are checked after bus drain.

## Focused validation and adversarial checks

- Initial combined Release build: zero warnings/errors; ten initial cases pass.
  A misspelled projection filter means that run does not prove the projection
  gate. The subsequent corrected runs explicitly include it.
- Expanded and final baseline: twelve cases plus
  `RequirementCoverageProjectionTests.CoreRequirements_MatchCompiledRequirementMetadata`,
  13/13 pass, zero failures/skips and zero build warnings/errors.
- Read-only reviewer `/root/outbox_proof_redteam` identified a missing negative
  compensation-failure event oracle. It was added; additional companion-event
  and transport-fault exclusions were added. No concrete test blocker remains.
- Counterprobe 1 omits compensation-outbox registration only in GATE: four
  compensation cases fail because attempt 0/1 effects reach PreSend while gated;
  all four execute controls pass. Build succeeds with zero warnings/errors.
- Counterprobe 2 passes the original activity token instead of the timeout token:
  four deadline cases fail at the exact cancellation boundary, eight other
  journey controls pass. Build succeeds with zero warnings/errors.
- Both changes are manually reversed. Original SHA-256 values are restored:
  `InMemoryOutboxConfigurationObserver.cs` =
  `6d0943a4748ebf53d88e9fe30239887e315a9ebd12cc91f6d5f0b2c47219b438`;
  `TimeoutActivityContextProxy.cs` =
  `6e3fc3cb00e71b677ff73623babd1958bc0e85e32f47ec26101a84777cb72516`.
- Restored new/existing controls: **69/69**, zero failures/skips, Release build
  zero warnings/errors. All **5,877** MAIN/GATE source/test paths match bytewise.
- Verify-only whitespace check loads Core and changes **0/5,727** files.
  No counterprobe remains applied.

## Sole complete measurement

| Metric | T48 | T49 |
| --- | ---: | ---: |
| Successful executions | 13,020 | **13,032** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Physical covered / valid lines | 85,703 / 93,753 | **85,771 / 93,753** |
| Line coverage | 91.4136080978742% | **91.48613911021514%** |
| Conservative covered / valid branches | 30,937 / 36,845 | **30,948 / 36,845** |
| Conservative branch coverage | 83.96525987243859% | **83.99511466956168%** |
| Method identities | 26,071 | **26,071** |
| Methods with CRAP > 30 | 0 | **0** |
| Line-gap identities | 4,375 | **4,333** |
| Zero / partial line coverage | 2,720 / 1,655 | **2,678 / 1,655** |
| Additional branch-only identities | 1,525 | **1,522** |
| Union of remaining gap candidates | 5,900 | **5,855** |

Every profile succeeds without failure or skip; all four fixture groups exit 0.
There is no retry measurement. Core increases from 6,761 to 6,773 cases; other
profile counts and CPU/provider selection are retained. CRAP uses per-method
complexity and unioned line coverage; conservative branches use the maximum
observed covered count per source line, not a union of stable branch identities.

## Reconciled observation changes

Physical observations gain **75** lines and lose **7**, net **+68**. Of the gains,
**44** belong to the nine primary targets, with no primary loss. Another **25**
are in the supporting outbox/timeout configuration and CompensateActivityFilter.
The six other gains are TaskExtensions (2), ResourceCache.Creation (1),
InMemoryDelayProvider (1) and ArrayPropertyConverter (2).

Supporting gains: InMemoryCompensateContextOutboxSpecification (10),
InMemoryOutboxConfigurationObserver (8), CompensateContextTimeoutSpecification (1),
ExecuteContextTimeoutSpecification (1), TimeoutConfigurationObserver (4), and
CompensateActivityFilter (1). The physical partition is
**44 primary + 25 supporting + 6 other = 75**.

Losses are outside the packet targets: EntityFrameworkReliableStore (1),
FutureExtensions (2), BatchConsumer (1), TypeConverterCache (1), and
ReliableMessagingDeliveryService.Telemetry (2). These observations are retained;
unrelated gains/losses are not attributed causally to the new tests or claimed
as new product defects.

There are **46** departures from the line-gap list and **4** new line gaps,
net **−42**. Branch-only membership has **6** departures and **3** additions,
net **−3**. These list transitions are not automatically fully covered methods;
their union decreases by **45**. No method/source-line identities are added or
removed, and no denominator is reduced.

Of the six branch-only departures, five become fully covered; one returns to
the line-gap list. The three branch-only additions comprise one transition from
the line-gap list and two newly observed gaps. Of the 46 line-gap departures,
45 become fully covered and one retains a branch gap. Thus 50 gap identities
become fully covered and five enter the union, explaining its net decrease of 45.

Conservative branches gain 11 overall. The nine primary sources have no net
branch gain. OutboxConfigurationObserver (+4), CompensateContextOutboxSpecification
(+2) and CompensateActivityFilter (+4) account for ten supporting gains; all other
observations net +1, without causal attribution to these tests.

## Independent final audit

Read-only reviewer `/root/outbox_proof_redteam` confirms 487 file hashes,
66 runner/settings bindings, nine broker-log hashes, all 33 profile receipts,
32 assemblies and all four clean fixture groups. It independently reconstructs
both T48 and T49 inventories from all 66 coverage XML files: all 26,071 method
rows per inventory, physical counts, CRAP values, gap lists and transition lists
agree exactly. No discrepancy or blocking finding remains. This is a separate
internal read-only review, not external product-team acceptance or global A+.

## Remaining risk and limits

| Highest CRAP among remaining line gaps | Complexity | Lines | CRAP | Uncovered lines |
| --- | ---: | ---: | ---: | --- |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 28 | 31/33 | 28.174527645602026 | 109, 110 |
| EventHubProducer.BatchSendPipe.SendAsync | 28 | 32/34 | 28.159576633421533 | 251, 300 |
| InMemoryReliableInboxContextFactory.SendAsync | 28 | 68/70 | 28.018285714285714 | 49, 127 |

The complete remaining set is in `artifacts/t49-method-gaps.json` and
`artifacts/t49-branch-only-gaps.json`; it includes compiler-generated identities,
not 5,855 proven independent defects. These named high-complexity paths and the
remaining behavior gaps still need contract-based disposition. No agreed numeric
A+ threshold is replaced by the diagnostic CRAP > 30 threshold.

These journeys prove volatile outbox behavior in a live in-memory process, not
crash durability or exactly-once external side effects. Exercising both flush
modes proves the asserted outcomes in both modes, not a throughput benchmark.
Activity cancellation currently surfaces through the transport-fault path;
the tests do not claim domain compensation completed after a timeout.
The deferred all-repository Roslyn API/comment audit remains after coverage/CRAP
completion. No global A+ completion is claimed.

## Artifact identity

Artifacts are retained in the ignored `artifacts/` directory; no raw TestResults
or review directory was modified. Optional HTML reports were not generated.

| Artifact | SHA-256 |
| --- | --- |
| t49-aggregate.json | `fb7ef097d46edb132ab5f7c7945fffac4a89799c12128ca574be5c7280e0160d` |
| t49-all-methods.json | `1a97b3a8a56d7a7b7bc61f77b5921f5aa9e294ff0e6cb472f4710e9aa7ecfb50` |
| t49-method-gaps.json | `4a45eea049cd5f3761095c048ba4bcbc406f6646b26c2425a4d0b4ab50b70f89` |
| t49-branch-only-gaps.json | `78153fb367da61b157d9de7bf5732c1ba8a679ccf79621a0d2affc21a0cc4024` |
| t49-profile-progress.json | `3f1893de31ca319f3117df4a766b62f8c823ce7fc19d6a7e35689e93aaadaf03` |
| t49-gap-delta.json | `0d51c899fc6ad582cf7bd0e83abe7b7013575b91d6f3e89dffb7faea7b7342ca` |

Additional artifacts bind pairing inputs, restored source hashes and counterprobe
logs. `t49-measure-all.py`, `t49-analyze-complete.py` and `t49-compare-gaps.py`
retain the exact orchestration and calculations.
