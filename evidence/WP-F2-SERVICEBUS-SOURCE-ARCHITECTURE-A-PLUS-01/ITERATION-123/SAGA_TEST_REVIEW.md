# Complete personal review of the saga Core-reading addition

Input: 3bb808bc999141638b1620c4adeb5bc51ceacc86; Core owner tree
e3be6b831183b3b65636c3b5e167c165696037d2. The main completely reads all twenty
files / 6,804 lines, including fields, nested messages, machines, handlers,
repository setup, distribution strategies and the scheduler fixture. This is a
read-only review of existing tests, not full Core-owner admission or new test design.

The scoped tests generally use meaningful outcome assertions, real in-memory
message pipelines, bounded gates and isolated harnesses. The settled findings are
0 Critical, 4 High, 0 Medium and 1 Low. Four High findings weaken a particular
oracle or regression failure path; none makes its entire test assertion-free.
All 93 test methods / 137 declared cases are accounted for below and exactly match
137 passed records in the actual final 4,007/4,007 Core run. Passing does not close
the review findings or prove actual mutant detection.

The test-anti-patterns skill and its .NET extension guide this handwritten review.
Its requested concrete correction targets are expressed as exact existing-contract
acceptance conditions. Implementing or designing replacement Core tests is deferred
until the complete 557-file owner and effective shared inputs have been admitted.
No generated disposition, source-to-test absence claim or sampled acceptance occurs.

## High findings and deferred exact correction targets

### H1 — Failed-response discard is never arranged

Location: SagaStateMachine/StateMachineResponseAndFaultIntegrationTests.cs:64,
Outbox_CommitsTheSuccessfulResponseAndPublishesOneFaultAfterAllRetriesAsync;
the nested OutboxMachine failed branch throws before responding.

The assertions meaningfully establish one successful response, six failing attempts,
one successful attempt, removal and one terminal typed fault. They do not prove
that a response queued by a failing attempt is discarded: there is no such response
in that branch. The failure could leak buffered responses without this arrangement
ever exercising the claimed condition.

Deferred correction target: the failing path must actually buffer a response before
its existing ExpectedOutboxFailure. Exact independently arranged outcomes remain
one OutboxStarted(successId, "committed"), zero failure responses, success attempts 1,
failed attempts 6, and exactly one terminal Fault for the arranged failing ID with
the ExpectedOutboxFailure type/message carried by the existing wrapper. Preserve
the current success/removal/retry assertions; do not weaken them to merely non-null.

### H2 — Published correlation IDs are taken from actual output

Location: SagaStateMachine/StateMachineResponseAndFaultIntegrationTests.cs:155,
Catch_RespondsAndPublishesBeforeEitherRetainingOrRemovingTheInstanceAsync;
the final published-record comparison uses published[0].CorrelationId and
published[1].CorrelationId in its expected objects.

The modes, number of publications, responses and retaining/removing behavior are
real assertions. The correlation portion is self-referential and can accept a
publication for the wrong saga. The arranged IDs currently live inside the try
scope and are unavailable to that final comparison.

Deferred correction target: retain the independently arranged removedId and
retainedId for the assertion scope. The exact expected records in mode-sorted order
are CatchPublished(removedId, "removed") and CatchPublished(retainedId, "retained").
Do not obtain either expected ID from a publication. Preserve exactly two responses
and the distinct removed-versus-retained repository assertions.

### H3 — The supposedly complete stale-event snapshot aliases mutable state

Location: JobService/StateMachine/JobAttemptGenerationTests.cs:27,
StaleAttemptEvents_LeaveTheCompleteSagaSnapshotUnchangedAsync; Snapshot helper and
JobSagaSnapshot record. Product neighbor JobSaga.cs is completely personally read.

The expected snapshot retains the saga's Job, Checkpoint and JobProperties
Dictionary references. In-place mutation changes the supposed expected value too;
record equality can then compare the same references. Additionally its eighteen
fields omit eleven productive properties: CorrelationId, JobSlotWaitToken,
JobRetryDelayToken, IncompleteAttempts, CronExpression, TimeZoneId, StartDate,
EndDate, NextStartDate, RowVersion and Version. The "complete" no-change promise
is stronger than its independently captured oracle.

Deferred correction target: independently capture all twenty-nine productive fields
with distinct applicable non-default values; copy dictionary/list/byte-array content
rather than retain mutable references. Existing maps must remain exactly Job =
{"label":"current-attempt"}, Checkpoint = {"checkpoint":"retained"} and
JobProperties = {"owner":"native"}, each with one entry, after every one of the
five stale events in each of the nine configured states. Assert all other arranged
fields against their independent pre-event values, including token, recurrence,
version, row-version and incomplete-attempt contents. No expected field may be
derived from post-event actual state. Current progress/lifecycle assertions remain.

### H4 — Removed-owner regression can hang before releasing its lease

Location: Sagas/InMemorySagaRepositoryConcurrencyTests.cs:11,
RemovedInstance_UnblocksAndRejectsEveryWaitingOwnerAsync; the waiter exception
assertion has no local bounded WaitAsync. Product SagaInstance.cs is completely read.

The first owner holds the instance lease, the second awaits its acquisition and
Remove marks the instance removed and cancels the removal signal. Remove itself
does not release the held lease. If cancellation no longer wakes the waiter, its
await never completes before the test's last-line Release. The ambient test token
is not a local regression bound. This can hang the host instead of giving a useful
failure; no hang is observed or deliberately injected in this iteration.

Deferred correction target: bound the waiter using the existing OperationTimeout
and current test cancellation token, and release the first lease in failure-safe
cleanup. Require the removed-instance InvalidOperationException contract and its
arranged correlation ID, not TimeoutException or cancellation. The existing internal
removal exception derives from InvalidOperationException, so ThrowsAny of that
specific base is legitimate here, not a blanket Exception assertion. This test
arranges one waiter; proving several queued owners is an adjacent scoped priority,
not a claim that no such test exists anywhere else in the repository.

## Medium and Low findings

| ID | Severity | Location | Disposition |
|---|---|---|---|
| L1 | Low | Sagas/LegacySagaIntegrationTests.cs:12 and its harness label | The retained scenarios exercise native message-saga capabilities, not an old MassTransit compatibility interface. Prefer a native message-saga label during the later owning edit. Preserve all nine methods / ten cases; the word Legacy is not evidence that a feature may be deleted. |

No Medium finding is assigned. Long integration scenarios, deliberate direct
repository composition, differentiated boundary cases and data-driven rows are not
automatically poor tests or duplication simply because they are large or low-level.

## Every test method accounted for

Each heading identifies its exact file beneath tests/ViciOne.ServiceBus.Tests.
The method rows below were manually written after complete code reading. Cases are
declared native cases, not invented tests. "No new finding" is a bounded review
disposition, never full-owner A+ acceptance.

### SagaStateMachine/StateMachineConcurrencyIntegrationTests.cs — 4 methods / 4 cases

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| AwaitedEnterActivity_FinalizesAndRemovesOnlyAfterTheExternalDecisionCompletesAsync | 1 | Real held asynchronous decision; before/after finalization and removal assertions. |
| HeldInstanceLock_DoesNotBlockAnotherInstanceAndRoutesAQueuedMessageThroughTheMissingPolicyAsync | 1 | Independent-instance progress, bounded gate and queued missing-policy outcome. |
| FourWayPartitioner_CreatesAllOneHundredInstancesAndRunsTheNestedMessagePipeExactlyOnceAsync | 1 | Exact hundred identities and nested-pipe cardinality. |
| ConcurrentVoices_UpdateOneInstanceWithoutLossAndReachHarmonyExactlyOnceAsync | 1 | Exact aggregate counts and single final harmony. |

### SagaStateMachine/StateMachineOutboxSchedulingIntegrationTests.cs — 1 / 1

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| SerializerFailureDuringScheduledOutboxDelivery_RetriesFromTheCommittedSagaStateAndCompletesOnceAsync | 1 | Actual serializer fault, committed-state retry and exact once completion. |

### SagaStateMachine/StateMachinePolicyIntegrationTests.cs — 5 / 5

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| SagaConfiguration_InvokesAllThreeScopesOnceWithTheSameSagaAndMessageAsync | 1 | All three scopes and identity/call-count assertions. |
| MissingStatus_RetriesAfterTheFirstAttemptAndSucceedsOnceTheInstanceExistsAsync | 1 | Held retry is released after actual instance creation. |
| IgnoredRepeatedEvent_IsConsumedWithoutFaultAndLeavesTheInstanceUnchangedAsync | 1 | Ignored event plus unchanged state and absence of typed fault. |
| RetryIgnoredException_ExecutesOncePublishesOneFaultAndKeepsThePriorStateAsync | 1 | Exact one attempt/fault and preserved state. |
| ContainerActivities_ExecuteTheFailureAndCatchStagesThenFinalizeAndRemoveAsync | 1 | Exact failure/catch activity execution and removal. |

### SagaStateMachine/StateMachineRequestIntegrationTests.cs — 2 / 6

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| ParallelRequests_ProduceTheExactCompositeOutcomeAsync | 4 | Four independent success/fault composite outcomes. |
| MultiResponseRequest_RoutesTheSecondAndThirdAcceptedTypesExactlyAsync | 2 | Exact second/third accepted types and metadata order. |

### SagaStateMachine/StateMachineResponseAndFaultIntegrationTests.cs — 5 / 5

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| RespondMatrix_ReturnsSyncAndAsyncResponsesAndFaultsTheMissingInstanceAsync | 1 | Exact response payloads and correlated missing-instance fault; completed async factory noted below. |
| Outbox_CommitsTheSuccessfulResponseAndPublishesOneFaultAfterAllRetriesAsync | 1 | H1; other exact retry/success/fault assertions remain meaningful. |
| MissingInstance_ReturnsOnlyTheConfiguredSubstituteResponseAsync | 1 | Exact substitute response and absence of ordinary saga execution. |
| Catch_RespondsAndPublishesBeforeEitherRetainingOrRemovingTheInstanceAsync | 1 | H2; modes, counts and remove/retain checks remain meaningful. |
| FaultEvents_ArePublishedCorrelatedAndConsumedByTheMachineOrMissingPolicyExactlyOnceAsync | 1 | Exact correlated fault routing and consumption counts. |

### SagaStateMachine/StateMachineSchedulingIntegrationTests.cs — 3 / 3

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| DataFaultHandlerSchedule_ForwardsTheConsumeCancellationTokenWhenReplacingThePreviousScheduleAsync | 1 | Correct cancellation token and replaced schedule ownership. |
| StateEventFaultHandlerSchedule_ForwardsTheConsumeCancellationTokenWhenReplacingThePreviousScheduleAsync | 1 | Distinct state-event overload retains exact token oracle. |
| CorrelatedSchedule_UsesTheInstanceDelayAndFinalizesAtTheExactAdvancedDeadlineAsync | 1 | Correlation/instance delay/finalization; deterministic boundary priority below. |

### SagaStateMachine/StateMachineTransportIntegrationTests.cs — 4 / 4

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| FactoryPublishAndSend_PreserveSagaIdentityPayloadAndTransportMetadataAsync | 1 | Exact message payload, saga identity and transport metadata. |
| RuntimeDeclaredEvents_CreateRunAndFinalizeTheSameInstanceAsync | 1 | Runtime declaration preserves instance throughout lifecycle. |
| DisabledConsumeTopology_LeavesTheSagaUntouchedWhileAnIndependentHandlerReceivesTheMessageAsync | 1 | Independent delivery and exact excluded-saga state. |
| WhenEnterRequest_IsSentCompletedAndAppliedBeforeTheMachineWaitsAsync | 1 | Request metadata and before-wait application ordering. |

### Sagas/Configuration/SagaConnectorTests.cs — 6 / 6

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| MessageContracts_AreDiscoveredInSemanticRoleAndStableMessageOrder | 1 | Exact role and message sequence. |
| DuplicateInitiatedAndOrchestratedRole_CreatesTheMissingSagaOnceAsync | 1 | Exact once creation despite two roles. |
| WritableCorrelationIdSaga_ReceivesTheMessageCorrelationIdBeforeConsumptionAsync | 1 | Independently arranged correlation assignment before consume. |
| SagaWithoutSupportedMessageContract_FailsWithOneActionableConfigurationError | 1 | Specific actionable configuration failure. |
| SagaWithOnlyAnUnsupportedMessageContract_FailsAsContractless | 1 | Unsupported-only construction is not silently accepted. |
| SagaWithoutASupportedConstructionShape_FailsWithOneActionableConfigurationError | 1 | Specific unsupported construction diagnostic. |

### Sagas/ContainerSagaIntegrationTests.cs — 2 / 2

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| ContainerSaga_ConsumesTheInitiatingOrchestratedAndObservedMessagesExactlyOnceAsync | 1 | Exact three semantic message roles and consumption counts. |
| SagaEndpointOverrides_RouteToTheInlineAndDefinitionOwnedAddressesAsync | 1 | Independent inline and definition-owned destinations. |

### Sagas/InMemorySagaRepositoryConcurrencyTests.cs — 2 / 2

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| RemovedInstance_UnblocksAndRejectsEveryWaitingOwnerAsync | 1 | H4; one arranged waiter, valid specific removal exception base. |
| DictionaryRemoval_InvalidatesTheRemovedInstanceAndAcceptsAReplacementAsync | 1 | Invalidated old instance and accepted replacement remain distinct. |

### Sagas/LegacySagaIntegrationTests.cs — 9 / 10

All methods retain functional value; L1 concerns only the file/type/harness label.

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| SagaConfiguration_InvokesAllThreePipeLayersWithTheSameSagaAndMessageAsync | 1 | Exact three layers with shared identity. |
| InitiatedByOrOrchestrates_CreatesAMissingInstanceAndReusesAnExistingInstanceAsync | 1 | Native creation and existing-instance reuse. |
| SagaExecuteFilter_AssignsTheExactDependencyBeforeTheInitiatingConsumeMethodRunsAsync | 1 | Exact DI object and before-consume assignment. |
| SagaFilterExpressionConverter_SubstitutesTheMessageAndExcludesTheOtherInstance | 2 | Independent matching and nonmatching instances. |
| PropertyExpressionSagaQueryFactory_ExtractsTheExactValueThroughBothPublicOverloads | 1 | Both public overloads' exact extracted value. |
| HandAssembledRepository_CreatesRoutesAndRemovesTheStateMachineInstanceAsync | 1 | Genuine supported direct repository lifecycle, not disposable compatibility. |
| HandAssembledInsertOnInitialRepository_FinalizesAndPublishesFinallyForEveryInstanceAsync | 1 | Exact per-instance finalization/publication. |
| DuplicateInitiatingMessage_PreservesTheInstanceAndPublishesOneTypedFaultAsync | 1 | Preserved identity and exact one typed fault. |
| DirectBusSagaConnection_ReportsTheTypedSagaExceptionForAnExistingInstanceAsync | 1 | Exact typed saga fault for the independently arranged existing instance. |

### Sagas/SagaMessageFilterBoundaryTests.cs — 1 / 1

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| MessageFilters_RejectEveryMissingPipelineArgumentBeforeConsumingAsync | 1 | Exact argument failures and zero downstream consumption. |

### Sagas/SagaPartitionerConfigurationTests.cs — 3 / 3

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| SagaPartitioner_GuidAndTextKeysUseTheirExactBinaryContractsAsync | 1 | Independently expected Guid and UTF-8 bytes, including Å; not encoder round-trip identity. |
| SagaPartitioner_RejectsANullTextKeyBeforeInvokingTheDownstreamPipeAsync | 1 | Specific null-key rejection and zero downstream calls. |
| SagaPartitionerOverloads_RejectEveryInvalidRequiredArgument | 1 | Exact parameter/type contracts across overloads. |

### Sagas/SagaRepositoryCapabilityTests.cs — 9 / 9

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| DispatchRepository_ExposesOnlyItsImplementedCapability | 1 | Unsupported capabilities stay absent. |
| LoadableRepository_ExposesAndForwardsOnlyTheRequestedAdditionalCapabilityAsync | 1 | Exact load forwarding and capability isolation. |
| QueryableLoadableRepository_ForwardsBothRequestedCapabilitiesAsync | 1 | Independent load/query paths and exact collaborator values. |
| RepositoryCreation_RejectsEveryMissingFactory | 1 | Specific required-factory guards. |
| DispatchOperations_RejectEveryMissingRequiredArgumentAsync | 1 | Exact async dispatch guard contracts. |
| LoadAndQueryRepositories_RejectEveryMissingRequiredArgumentAsync | 1 | Exact load/query required parameters. |
| InMemoryRegistration_DoesNotRegisterTheEndpointDispatchRepositoryAsAnApplicationService | 1 | Scope ownership enforced, not an accidentally exposed application service. |
| SagaRegistration_RejectsAMissingRepository | 1 | Specific registration failure. |
| RepositoryOnlyRegistration_RejectsAMissingRepository | 1 | Distinct repository-only registration guard. |

### JobService/StateMachine/JobAttemptGenerationTests.cs — 7 / 32

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| StaleAttemptEvents_LeaveTheCompleteSagaSnapshotUnchangedAsync | 9 | H3; stale-event oracle is not complete or independent for mutable contents. |
| CurrentAttemptEvent_StillUsesTheConfiguredStateRuleAsync | 1 | Current generation still follows the configured rule. |
| NewerProgressSequence_ReplacesTheStoredProgressAndSequenceAsync | 1 | Exact advancing sequence and progress payload. |
| NonIncreasingProgressSequence_PreservesStoredProgressAsync | 2 | Equal and lower sequences preserve stored progress. |
| TerminalAttemptEvent_AppliesTheCompleteCheckpointUpdateContractAsync | 9 | Exact keep/clear/replace semantics, copy ownership and post-input-mutation checks. |
| LifecycleStatus_MapsEveryReachablePersistedState | 9 | Independently expected lifecycle enum per persisted state. |
| LifecycleStatus_MapsInitialAndMissingStatesExplicitly | 1 | Exact initial/missing mapping, not merely enum membership. |

### JobService/StateMachine/JobAttemptStateMachineTests.cs — 9 / 13

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| StartAttempt_SchedulesLivenessAndForwardsTheCompleteJobAsync | 1 | Exact liveness/start payload and snapshot ownership. |
| UnansweredStatusChecks_EscalateAndApplyTheRetryDelayBoundaryAsync | 2 | Before/at retry escalation boundary and scheduled behavior. |
| StatusResponse_SelectsItsExactStateAsync | 4 | Exact status/state mapping; canceled-token identity priority below. |
| StartFault_PreservesTheFirstExceptionAndCanBeFinalizedAsync | 1 | Original exception and finalization retained. |
| CancellationCommand_IsForwardedAndCompletionFinalizesAsync | 1 | Exact forwarded cancellation and terminal state. |
| StartFault_RejectsMissingExceptionDetailsAsync | 1 | Specific missing-detail failure. |
| ReportedAttemptFault_RecordsTimeAndStopsLivenessAsync | 1 | Exact fault/time/liveness effects; token identity priority below. |
| LateStartAcknowledgement_EnrichesButDoesNotReopenAFaultedAttemptAsync | 1 | Late enrichment preserves terminal faulted state. |
| StartingTimeout_ReportsTheAssignedInstanceAndRetryPolicyAsync | 1 | Exact instance and retry-policy timeout payload. |

### JobService/StateMachine/JobStateMachineLifecycleTests.cs — 11 / 14

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| ImmediateSubmission_SnapshotsValuesAndRequestsCapacityAsync | 1 | Independent maps and exact capacity request. |
| ExhaustedInitialSchedule_HonorsFinalizationPolicyAsync | 2 | Both remove/retain policies preserve exact outcome. |
| CalculateNextStartDate_CoversOneTimeTimeZoneAndEndWindowBoundariesAsync | 1 | Known chronological instants and end/time-zone boundaries. |
| ScheduledOccurrence_FinalizesOldAttemptsAndStartsANewGenerationAsync | 1 | Exact old-attempt drainage and new generation. |
| RecurringFault_ReleasesCapacityAndSchedulesTheNextOccurrenceAsync | 1 | Capacity/state meaningful; independent next-due oracle priority below. |
| AttemptStartFault_PreservesAndRoutesTheFailureAsync | 1 | Exact original failure and routed payload. |
| PendingCapacityOutcome_CompletesCancellationAsync | 3 | Three capacity outcomes retain cancellation contract. |
| ShutdownCancellation_ReleasesCapacityAndReturnsToWaitingAsync | 1 | Exact release and waiting transition. |
| TerminalLateEvents_PreserveCurrentDetailsAndNotificationsAsync | 1 | Terminal generation/details and exact notification behavior. |
| TerminalFinalization_DrainsEveryIncompleteAttemptAsync | 1 | Exact complete set of incomplete attempts drained. |
| CompletedScheduleWithoutNextOccurrence_HonorsFinalizationPolicyAsync | 1 | Exhausted next occurrence follows configured policy. |

### JobService/StateMachine/JobTypeCapacityTests.cs — 6 / 12

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| RemoveExpiredAllocations_PreservesOnlyLiveAllocationsOnKnownInstances | 1 | Exact known/live retained allocations. |
| RemoveExpiredAllocations_RejectsMissingStateAndNonPositiveTimeouts | 1 | Distinct null/zero/negative boundary guards. |
| JobAllocationState_UsesOnlyJobIdentityForEqualityAndHashing | 1 | Independently varied nonidentity fields and exact equality/hash contract. |
| JobDistributionContext_IsImmutableAndIsolatedFromPersistedState | 1 | Read-only mutation rejection and independently isolated contents. |
| ValidateConcurrencyUpdate_RejectsEveryInvalidInvariant | 7 | Seven exact invalid invariant categories. |
| ValidateConcurrencyUpdate_AcceptsEveryDefinedUpdateKind | 1 | Void validator's required normal return is meaningful, paired with invalid-case assertions; not assertion-free false confidence. |

### JobService/StateMachine/JobTypeStateMachineTests.cs — 4 / 5

| Completely reviewed method | Cases | Scoped disposition |
|---|---:|---|
| ConcurrencyUpdates_PreserveTheCompleteInstanceAndLimitStateAsync | 1 | Exact instance/limit updates. |
| Allocation_IsIdempotentAndEnforcesGlobalCapacityAsync | 1 | Duplicate allocation and global capacity boundaries. |
| RegisteredDistributionStrategy_ControlsAllocationAsync | 2 | Both registered selection outcomes observed, not merely verified callback invocation. |
| SuspectRelease_RemovesTheAllocationAndItsInstanceAsync | 1 | Exact allocation and instance removal. |

### JobService/StateMachine/StateMachineTestScheduler.cs — fixture, 0 tests / 0 cases

All 153 lines are read: scheduled/canceled capture lists, request/response payloads,
configured logical due dates, explicit token capture, send/publish dispatch helpers,
pipe application and cancellation forwarding. This deterministic fixture supplies
observable collaborator outcomes; it is not itself a shipping provider or a real
cloud acceptance boundary. Its full reading is counted once in the twenty files.

## Adjacent scoped proof priorities

These priorities qualify the reviewed arrangements; they are not additional settled
findings in the five-item count and do not assert absence in unread repository files.

1. Scheduling deadline: CreateDeadline uses DateTime.UtcNow, while the actual delay
   provider uses TimeProvider.System plus its manually advanced offset. Immediately
   asserting timeoutDelivery.IsCompleted is false after advancing to one tick before
   due does not establish that asynchronous dispatch has settled. Close the exact
   boundary with an owned fake-clock seam and deterministic due/dispatch observation,
   not sleeps. No flaky execution or timing mutant is observed here.
2. Attempt status/fault/finalization: several cases assert a single canceled schedule
   without proving its identity is the stored StatusCheckTokenId. Preserve existing
   states/counts and require the independently arranged original token plus cleared
   storage. Inspect sibling coverage before choosing an owning change.
3. Recurring fault: equality between NextStartDate and ScheduledDueAt may compare two
   computed outputs rather than an independently expected chronological instant.
   Other reviewed date-boundary cases already use exact instants; unread Cron tests
   must be reconciled before a repository-wide absence claim.
4. Response factory: Task.FromResult proves completed asynchronous forwarding, not
   suspension/cancellation/failure while held. The concurrency suite already has a
   genuine held enter callback, so the repository is not universally completed-task
   only. Reconcile sibling suspended callback contracts and cancellation ownership.
5. Parallel/multi-response request: four composite outcomes and second/third accepted
   types are already independently checked. Unknown or duplicate replies, accepted
   type ordering and independent URN-encoding expectations require sibling review;
   do not infer global gaps from this scoped reading.
6. Retry/observers/cleanup: exact ignored-exception single execution and held retry
   progress are present. Observer cancellation, disposal masking, multiple queued
   saga owners and scheduling cancellation recovery remain connected runtime proof
   work, not new executed regressions or closed source findings.

## Positive observations and category closure for this read scope

The complete reading checks assertions, awaited operations, resource cleanup,
isolation, naming, structure, clock/random dependencies and collaborator ownership.
It identifies no missing awaited async assertion, swallowed test exception,
always-true assertion, commented-out oracle or systematic coverage-touching pattern
in these twenty files. No mock-framework over-mocking pattern is found. Required
guard cases check specific exception types/parameter names and zero downstream work.
Real in-memory integration, bounded completion gates, using/await using and finally
cleanup are prevalent. Exact partition bytes distinguish UTF-8 from incompatible
encoding, independent request outcomes preserve accepted-type order, and checkpoint
replacement tests explicitly check different references and mutate original inputs
after capture. Snapshot tests are not all equally independent: H3 remains open.

Priority after full owner admission: correct H3 and H2 independent oracles, H1 actual
failure arrangement and H4 bounded failure cleanup; prove their selected causal
regressions with real compilation/execution and exact restoration. L1 is opportunistic
native naming, never a reason to delete retained saga functionality. All related
runtime and whole-product API/coverage/provider obligations remain active.

## Actual reviewed-method and final-case receipts

Read-only enumeration receipt, not a generated review:
/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/reviewed-test-methods.log,
SHA256 395a3438dd890b4ea7d7372877abd156091e0c96e2123c3aec5b897ca7d5e5f0.
Final independently parsed native-case receipt terminates 0:
/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/final-native-case-validation.log,
SHA256 043008c5f436542316d5430b76580cae2c0228d32e7b2285c361a7634c9b0647.
It verifies 4,007 actual passed records and the exact 93 unique reviewed methods /
137 declared passed cases. Initial use of Ruby filter_map terminates 1 because the
system Ruby lacks that method; its raw redirected file is empty and preserved as
final-native-case-validation-initial-empty.log. Switching only the read-only
diagnostic to map/compact resolves it. This is not a product/test failure, nor a
reason to rerun already successful builds or hosts.
