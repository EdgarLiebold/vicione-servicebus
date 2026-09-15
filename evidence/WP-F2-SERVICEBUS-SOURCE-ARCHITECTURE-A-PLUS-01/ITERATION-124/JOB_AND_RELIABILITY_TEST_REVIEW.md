# Job and reliability test review

Scope: the forty test files and five standalone fixtures in the main's complete
45-file personal reading packet. Input commit:
`10d1198187cf7ab1050351cfc0cac97489111b79`; unchanged Core tree:
`e3be6b831183b3b65636c3b5e167c165696037d2`.
[The sorted reading manifest](CORE_OWNER_READ_PROGRESS.md) binds all 11,805 lines
and every file's input/final SHA256. The entire 557-file Core owner remains
partially read: 92 files read, 465 remaining. This is a scoped review of existing
tests, not new test design, complete-owner Lead acceptance or a product certificate.

The main completely reads test-anti-patterns, test-analysis-extensions and its
.NET reference before reviewing. Their guidance informs complete method/helper
accounting, calibrated severity, concrete independent correction targets,
positive contracts and adjacent-proof qualification. The review and every
disposition below are manually authored, not generated from static signals.
No new internal advisor, external Red Team or product-role acceptance is claimed.

## Settled findings

**0 Critical / 4 High / 1 Medium / 1 Low. All six remain open.** These are
existing scoped test weaknesses, not six demonstrated productive defects.
Passing their current cases is not an effective mutation proof. Correction
targets below are review acceptance criteria; replacement test code is deferred
until the complete owning test project is admitted under Agreement §4.3.

### JR-01 — High: lifecycle deserialization does not bind the input payload

`JobService/Api/JobServiceEventExtensionsTests.cs:12`,
`GetJob_DeserializesEveryLifecycleEventFromItsOwnPayload` arranges five different
lifecycle contract contexts and an `EventJob("invoice-42")` result. Its serializer
proxy checks only that the input is assignable to IReadOnlyDictionary and that
the requested type is EventJob, then unconditionally returns the fixed result.
It does not compare the serializer input with the independently arranged
dictionary. Passing another dictionary can preserve all current assertions.

Required independent target: for each IStartJob/IFaultJob/ICompleteJob/
IJobCompleted/IJobFaulted context, require the exact arranged payload instance
at the serializer call and preserve the exact returned result/type. Reject a
different payload even if its shape or values are similar. Retain all five
null-context guards. This is not a claim that all deserializer tests are weak:
`StartJobConsumerTests.MatchingCommand_DeserializesAndForwardsEveryAdmissionInputAsync`
already asserts the exact independently arranged serialized input.

### JR-02 — High: captured dispatcher configuration can be disconnected

`JobService/Configuration/JobConsumerKindContractTests.cs:96`,
`DispatcherAndHarness_UseOnlyTheJobConsumerIdentity` observes the expected queue,
returned dispatcher, harness consumer identity and a non-null captured Configure
delegate, but never executes that delegate. An empty or wrong-consumer callback
can satisfy those observations without configuring the requested job consumer.

Required independent target: invoke the captured configuration with separately
arranged endpoint and registration-context owners, require FirstJobConsumer's
registration exactly once with those same owners, and require zero ordinary/
SecondJobConsumer configuration. Keep exact queue, dispatcher and harness checks
and required-input guards. The sibling Planning and RuntimeAndBulk methods
actually execute their own configuration paths and independently check counts;
they do not automatically prove this dispatcher callback path.

### JR-03 — High: broken waits/traversal can hang instead of failing boundedly

The following fully read cases contain locally held work or progress-dependent
traversal without an independent failure-safe completion/progress bound:

- `ConsumeJobContextCancellationTests.CancelJobHandle_ReleasesLocalOwnershipWhenExecutionIgnoresCancellationAsync`
  and `StartJobAsync_SnapshotsTheCancellationDeadlineBeforeInvokingTheConsumerPipeAsync`:
  after virtual time advances, cancellation and handle completion are awaited
  without the existing OperationTimeout. Held execution is released only on the
  successful tail of the snapshot case, not in finally.
- `JobServiceBusObserverTests.Observer_StartsAfterReadinessAndStopsBeforeTheBusAsync`:
  the readiness gate is released and startup awaited without a local bound.
- `JobServiceLifecycleTests.StartAndStopTransitions_AreSerializedInBothDirectionsAsync`
  and `RepeatedStarts_CancelAndReplaceHeartbeatGenerationsWithoutOverlapAsync`:
  assertions occur with real lifecycle/heartbeat gates held; not every held gate
  has a failure-safe release. Preserve their actual exclusion/order evidence.
- `DurableSenderDeliveryTests.BatchClaim_StartsNoMoreThanTheConfiguredConcurrentDeliveryLimitAsync:246`:
  two deliveries wait on BlockingDispatcher.Release; assertions precede its
  success-only release and the batch is then awaited without a local bound.
- `InMemoryReliableStoreTests.QuarantinePagination_TraversesMoreThanOneThousandEqualTimestampsWithoutDuplicatesOrGapsAsync:367`
  and `InboxQuarantine_TraversesMoreThanOneThousandRowsAndAppliesExplicitOperatorActionsAsync`:
  pending-claim and continuation loops assume progress. Empty claims with a
  still-pending snapshot, or a repeated continuation, can loop indefinitely.

Required independent targets: bound the relevant waits by the existing validated
OperationTimeout and caller/test cancellation policy, release held execution and
dispatcher/lifecycle gates in finally, and drain their work before disposing its
owners. For ignored cancellation, assert the execution is still incomplete at
the required observation point before cleanup releases it. For the deadline
snapshot, preserve the exact one-minute versus mutated one-day distinction.
For durable delivery preserve dispatch count/concurrency 2, stored count 3→1→0
and final dispatch count 3. For the 1,005-row traversal require each setup claim
to be nonempty, contain only arranged IDs and make unique progress; traversal
must not exceed 1,005 useful setup iterations. With fixed page size 137, at most
eight nonempty traversal pages are needed; require progress/no repeated cursor
and preserve the complete independent 1,005-ID ordered sequence and uniqueness.

No actual hang occurred in the historical passing run. This finding concerns
bounded failure behavior under a broken implementation or future mutation.
Do not mechanically flag every await or deliberate hold: CountingJobPipe's
bounded ManualResetEventSlim is intentional synchronous-admission-race evidence;
matching cancellation catches signal and rethrow. Existing Stop_CancelsAndDrains
and AdmissionFixture cleanup already use finally, and the reliable in-memory
integration cases stop the harness before asserting final event counts.

### JR-04 — High: duplicate admission does not exclude rejected pipeline work

`JobService/JobService/JobServiceLifecycleTests.cs:340`,
`ActiveJobIdentity_CannotBeAdmittedTwiceAsync` checks the exact conflicting JobId
and continued presence of the original handle. It constructs the rejected
CountingJobPipe inline and never observes its Count. Invoking that pipe before
throwing the expected JobAlreadyExistsException can leave these assertions green.

Required independent target: retain an independently observed rejected pipe,
require Count zero, require the original handle/execution identity remains the
owner, and preserve the exact JobId and subsequent original-execution cancellation.
The separate synchronous-pipeline-failure case proves reservation release and
successful replacement after failure; it is not zero-work duplicate rejection.

### JR-05 — Medium: default-off journal checks an unreachable store

`MessageJournal/MessageJournalIntegrationTests.cs:17`,
`WithoutExplicitConnection_TheJournalPerformsNoWorkAsync` constructs a local
RecordingStore but never connects or registers it with the harness. Its zero
append/empty/not-completed observations are true for that unreachable store
regardless of whether some other journal observer performs work. Actual message
delivery is observed, but that alone does not prove the named no-observer claim.

Required target: preserve real successful delivery and independently observe
the relevant default composition/observer activation boundary. Require no selected
journal observer/provider work in that actual composition; do not use an unused
local store as its sole oracle. Alternatively narrow the declared contract to
what is actually observable. Do not remove the default-off feature or weaken its
required proof. The connected NullProjection writer case genuinely verifies zero
work on its actual store, and connected outgoing/consume integration cases prove
real journal writes; neither makes this disconnected default-off oracle sufficient.

### JR-06 — Low: compact fixture formatting is inconsistent

The full JobProgressBufferTests notification fixture and JobServiceLifecycleTests
ControlledPublishEndpoint contain complete one-line method blocks inconsistent
with the readable surrounding declarations. This is a cosmetic maintainability
finding, not proof of incorrect behavior or productive dummy elements. Apply the
repository's established formatting after complete test-owner admission; retain
all forwarding, cancellation and deliberate unsupported-operation behavior.

## Positive contracts and adjacent proof priorities

The existing packet provides meaningful evidence rather than only coverage
touches. Typed submission/scheduling/recurrence checks assert identities, jobs,
initializer values, properties and cancellation; direct request clients arrange
accepted identities separately. RecordingSubmitJobClient returning the request
identity is a legitimate forwarding fixture, not automatically a self-oracle.
The dedicated independent SHA256 identity golden vector is
`760c9b52-8751-a488-7035-4a755b605360`; related factory-consistency checks do not
replace it or become Critical merely because both use the same identity factory.

Job context tests independently observe six notification cancellation paths,
metadata snapshots and 64 concurrent progress reports. Real integration tests
exercise retry with distinct attempt IDs, checkpoint restoration, cancellation
during running/waiting/allocation, exact typed terminal contracts, scoped
publication, finalization and recurring identity continuity. ConsumedCountAsync
and PublishedCountAsync use Take(expectedCount): they are prefix observation
barriers, not independently complete no-duplicate event-set proofs. The reliable
in-memory inbox/outbox cases separately stop/drain the harness before exact
execution, routing-key, 100-event and saga terminal-state assertions.

Store tests independently check count/byte admission, eight immutable-intent
conflicts, caller buffer snapshots, ordered claims, lease expiry/takeover,
generation fencing after readmission, due-time boundaries and full 1,005-row
quarantine traversal. Dispatcher acceptance/completion results in the delivery
unit fixtures are controlled protocol simulations. They are not evidence that a
real broker, database transaction or remote consumer accepted a send.

Journal writer tests independently observe the sanitization boundary, null
projection exclusion, escaped UTF8-size accounting (including exact size 166),
detached body backing storage, virtual-clock timeout and contained provider/
clock/capture/policy failure. Telemetry tests assert complete low-cardinality
schemas, eight explicit failure reasons, activity lifetime and listener isolation;
their collection isolates global listeners. Do not misclassify intentional
containment tests or matching cancellation rethrows as swallowed SUT failures.

Cron tests use independent field arrays, complete calendar samples, named exact
UTC instants and custom deterministic DST zones. ParsedField_ContainsAtLeastOneValue
has a narrow nonempty-fields contract; exact fields are separately covered by
parsing/calendar tests. Do not inflate its NotEmpty into a systematic Critical
finding. TimeZoneResolver observes independent owner resolvers and exact call counts.
Job property tests check complete case-insensitive mutation/read-only contracts
and independently arranged snapshots, not only round trips.

Adjacent priorities, not newly settled global gaps: held/failing notification
callbacks; foreign-token cancellation; complete no-extra-work proofs around
prefix barriers; deterministic recurring scheduling before/at due instants;
exact endpoint filter multiplicity and physical custom-repository use; custom
provider malformed results and cancellation across every operation; durable
claim-store fault/lease recovery; journal all-selected-observer disconnect and
post-terminal observation; multibus schedule persistence behavior rather than
only resolved scheduler type. StartupValidation intentionally accepts failures
at composition materialization or startup because both are fail-closed boundaries;
its broad catch plus message check is not blindly reported as swallowed failures.
The exact exception domain/zero-background-work proof still needs causal source
closure. None of these priorities claims repository-wide absence or effective kills.

All standalone fixtures are fully read: ApiJob, RecordingPublishEndpoint,
RecordingSubmitJobClient, ResponseFactory and ContainerJobDiscoveryTypes. The
many nested proxies, gates, store wrappers, policies, consumers, saga, clocks,
recorders and data providers are also fully read, not excluded from line totals.
Intentional test-only unsupported operations, null missing-collaborator fixtures,
controlled failure types and no-op observer callbacks are not productive dummy
features or MassTransit compatibility evidence. No productive legacy removal,
type/folder rename, comment generation or shipping API acceptance is performed.

## Complete manual method accounting

Each method below is personally reviewed in its complete file and helpers.
Cases are independently reconciled to iteration-123's historical native report;
they are not new executions or letter grades. Observations are scoped to the
named assertion contract, not blanket approval of unreviewed neighbors.
Paths are relative to `tests/ViciOne.ServiceBus.Tests/`; `.cs` owner is stated
by each heading. All 283 methods / 514 cases are accounted once below.

### DurableSend/DurableSenderDeliveryTests — 13 methods / 13 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| TransportAcceptance_RetiresThePersistedIntentAsync | 1 | Controlled acceptance retires count/bytes; not real broker acceptance. |
| ConsumerCompletion_KeepsCapacityUntilTheLogicalConsumerCompletesAsync | 1 | Capacity retained until matching logical completion. |
| ConsumerCompletion_MayWinBeforeTheAwaitingStateTransitionAsync | 1 | Early completion wins the persistence transition race. |
| ConsumerCompletion_WinsAnOverlappingAmbiguousDispatchFailureAsync | 1 | Completed generation survives ambiguous dispatch failure. |
| MissingConsumerCompletion_RedispatchesOnlyWithinTheAttemptBudgetAsync | 1 | Due-time redispatch and terminal budget quarantine. |
| LateConsumerCompletion_RetiresTheSameGenerationAfterTimeoutQuarantineAsync | 1 | Late same-generation completion releases quarantine. |
| TransportFailure_OnlyClassifiedTransientFailuresRetryAndExhaustionQuarantinesAsync | 1 | Classified transient retry then exhaustion. |
| TransportFailure_PermanentAndUnknownClassificationsNeverEnterRetryAsync | 1 | Permanent/unknown/classifier failure remain nonretryable. |
| UnsupportedCompletionMode_FailsClosedIntoInvariantQuarantineAsync | 1 | Undefined completion result quarantines invariant failure. |
| StatePersistenceFailure_AfterSuccessfulDispatchEscapesWithoutDeletingTheIntentAsync | 1 | Exact persistence fault escapes; intent retained. |
| BatchClaim_StartsNoMoreThanTheConfiguredConcurrentDeliveryLimitAsync | 1 | Real concurrency gates and capacity; JR-03 cleanup/bounds. |
| ProviderClaims_InvalidBatchesFailBeforeTheFirstDispatchAsync | 1 | Malformed claims reject before dispatcher work. |
| RetryDelay_RemainsBoundedAndDecorrelatedAtTheBackoffCeiling | 1 | Backoff samples stay bounded and decorrelated. |

### DurableSend/InMemoryReliableStoreTests — 17 methods / 17 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Admission_ExactDuplicateIsIdempotentWithoutASecondCapacityChargeAsync | 1 | Exact duplicate preserves single count/byte charge. |
| Admission_SameIdWithAnyDifferentImmutableIntentFailsLoudlyAsync | 1 | Eight independently changed intent fields conflict. |
| Admission_ConcurrentWritersCannotOvershootCountCapacityAsync | 1 | 100 concurrent attempts yield exactly ten admissions. |
| Admission_UsesExactBodyPlusMetadataBytesAndStillCountBoundsZeroByteRecordsAsync | 1 | Exact byte edge and zero-byte count bound. |
| Admission_SnapshotsCallerOwnedBodyAndMetadataBuffersAsync | 1 | Source buffer mutation cannot change retained arrays. |
| Claim_ExcludesLiveLeaseAndIssuesANewTokenAtExpiryAsync | 1 | Exact expiry takeover and stale lease rejection. |
| Claim_IsBoundedOrderedAndRejectsUnsafeRequestsAsync | 1 | Independent ordered IDs and unsafe claim guards. |
| Retry_PersistsBoundedEvidenceAndBecomesClaimableOnlyWhenDueAsync | 1 | Retry attempt/status and before/at due boundary; failure-text detail separate. |
| Quarantine_RetainsCapacityUntilDiscardAndRequeueDoesNotDoubleChargeAsync | 1 | Retention, requeue, terminal discard and capacity release. |
| Completion_ValidGenerationWinsQuarantineRaceButStaleGenerationCannotRetireReadmissionAsync | 1 | Old generation cannot retire a new admission. |
| Completion_MayRetireBeforeTheAwaitingTransitionIsPersistedAsync | 1 | Early completion prevents subsequent await transition. |
| QuarantineQuery_ReturnsOnlyTheBoundedNewestPayloadFreeEvidenceAsync | 1 | Newest evidence, failure-text 512 bound and page limits. |
| QuarantinePagination_TraversesMoreThanOneThousandEqualTimestampsWithoutDuplicatesOrGapsAsync | 1 | Independent 1,005-ID traversal/concurrent mutation; JR-03 progress bound. |
| Schedule_BecomesClaimableAtDueTimeAndCanBeCanceledBeforeClaimAsync | 1 | Tick-before/exact-due and cancellation state boundaries. |
| Inbox_DuplicateAndRetryTransitionsAreDueAndLeaseFencedAsync | 1 | Busy/not-due/retry/stale lease/completed duplicate. |
| InboxQuarantine_TraversesMoreThanOneThousandRowsAndAppliesExplicitOperatorActionsAsync | 1 | Complete 1,005-row traversal/actions; JR-03 progress bound. |
| Operations_PreCanceledTokenIsPreservedWithoutMutationAsync | 1 | Admission token identity and zero mutation, not all operations. |

### DurableSend/ReliableMessagingProviderGuardTests — 2 methods / 2 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| IdentityBoundResults_RejectDefaultsMismatchesAndMissingInstances | 1 | Missing/default/wrong identity rejected; valid result identity preserved. |
| QuarantinePages_RejectMissingOversizedAndMalformedContinuations | 1 | Missing/oversized/mismatched continuation rejected; valid page same. |

### DurableSend/ReliableMessagingRegistrationAndAdmissionTests — 22 methods / 31 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| TypedSender_UsesTheCanonicalRouteAndSerializerBeforeOfflineDurableAdmissionAsync | 1 | Offline retention, route duplicate and serialized identity/headers. |
| TypedSender_MultiBusKeepsFacadeRouteStoreAndPersistenceIdentityIsolatedAsync | 1 | Separate routes, payloads, stores and persistence identities. |
| MessageScheduler_PersistsDueAtAndCancelsThroughTheReliableStoreAsync | 1 | DueAt/cancel and replayed exact metadata/TTL. |
| MessageScheduler_MultiBusRegistrationsRemainIsolatedAsync | 1 | Separate resolved typed owners; runtime isolation priority separate. |
| MessageScheduler_RejectsASecondExplicitAdapter | 1 | Duplicate explicit adapter fails configuration. |
| MessageScheduler_TransportAdapterIsTheResolvedSchedulerAsync | 1 | Explicit adapter resolves without store-scheduler fallback. |
| TypedSender_UsesMessageDataOffloadEvidenceForPayloadAdmissionAsync | 1 | Large value absent from retained envelope; address present. |
| TypedSender_EnforcesPayloadAdmissionBeforePersistentMutationAsync | 1 | Exact configured serialized-body limit and no retained record. |
| Admission_RegisteredIdentityUsesTheConfiguredLimitsAndApplicationClockAsync | 1 | Exact application clock, store limits, ID and call count. |
| Admission_InvalidProviderResultsFailClosedAsync | 1 | Four malformed results fail without inner store mutation. |
| Admission_UnknownStableIdentityIsRejectedBeforeAnyStoreMutationAsync | 1 | Unknown contract fails with zero provider calls/storage. |
| MessageContracts_ComposeAdditiveDeclarationsIntoOneImmutableCatalog | 1 | Additive composition, duplicate stability, conflicts and preownership. |
| InMemoryDispatcher_RejectsDuplicateOwnerButKeepsTypedBusesIndependent | 1 | One owner per bus and duplicate/preowned rejection. |
| Registration_InvalidRuntimePoliciesFailClosedWhenTheTypedSenderMaterializes | 1 | Fifteen explicitly invalid safety policies fail closed. |
| StartupValidation_RejectsEveryIncompleteOrAmbiguousCompositionAsync | 10 | Ten invalid compositions fail at materialization/startup; see qualification. |
| Operations_ValidatePagesAndUseTheInjectedClockForOperatorTransitionsAsync | 1 | Bound page and exact requeue-clock due transition/discard. |
| Operations_ReadContractsValidateBeforeProviderAndForwardExactInputsAsync | 1 | Exact query/token forwarding and zero additional invalid calls. |
| Operations_ApplyInboxTransitionsAndRejectOutboxAbandonWithoutLosingStateAsync | 1 | Exact inbox action states and invalid outbox abandon. |
| Operations_AbandonLogsAndMetersOnlyTheAppliedRetainedDecisionAsync | 1 | One applied retained decision, one log and metric increment. |
| HealthCheck_ReportsCapacityWithoutMessagePayloadOrIdentityAsync | 1 | Healthy→degraded and bounded nonsensitive data keys. |
| HealthCheck_DistinguishesOperationalDegradationFromHardBoundViolationAsync | 1 | Quarantine/age degraded versus capacity unhealthy. |
| HealthCheck_SanitizesStoreFailuresAndPreservesCallerCancellationAsync | 1 | Fault type sanitized, secret message omitted, exact canceled token. |

### JobService/Api/JobServiceContractArchitectureTests — 4 methods / 4 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| PublicContracts_DoNotExposeMutableDictionaryTypes | 1 | Enumerated public job contracts reject mutable dictionary surfaces. |
| SerializerRepresentations_AreNotPartOfThePublicApi | 1 | Representation types are not public API. |
| JobServiceConfiguratorImplementation_IsNotPartOfThePublicApi | 1 | Implementation accessibility kept internal. |
| RetryConfiguration_DoesNotAdvertiseDisconnectedObservers | 1 | No disconnected observer configuration method. |

### JobService/Api/JobServiceEventExtensionsTests — 2 methods / 2 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| GetJob_DeserializesEveryLifecycleEventFromItsOwnPayload | 1 | Result/type checks; JR-01 exact input missing. |
| GetJob_RejectsMissingLifecycleEventContexts | 1 | Five missing-context guards. |

### JobService/Api/JobServiceExceptionTests — 2 methods / 2 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| JobAlreadyExistsException_PreservesTheConflictingIdentifier | 1 | Exact conflicting ID preserved. |
| JobServiceStoppingException_PreservesTheRejectedIdentifier | 1 | Exact rejected ID preserved. |

### JobService/Api/JobServiceExtensionsTests — 8 methods / 10 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| SubmitJobAsync_TypedPublishPreservesTheCompleteCommandAsync | 1 | Explicit job, identity, properties and token forwarded. |
| SubmitJobFromValuesAsync_RequestInitializesTheTypedContractAsync | 1 | Initializer becomes correct request contract. |
| SubmitJobAsync_GeneratedIdentifiersAreNonEmptyAndDistinctAsync | 1 | Independent nonempty/distinct generated IDs. |
| GeneratedSubmissionOverloads_ForwardJobsValuesPropertiesAndAcceptedIdentityAsync | 1 | Typed/value variants and separately accepted identity. |
| SubmissionApis_RejectNullJobsAndInitializerValuesAsync | 1 | Required jobs/values fail before endpoint work. |
| SubmissionApis_RejectEmptyExplicitIdentifiersAsync | 1 | Empty explicit IDs rejected across variants. |
| LifecycleCommandApis_RejectInvalidRequiredArgumentsAsync | 1 | Required lifecycle input guards. |
| CancelJobAsync_MissingReasonUsesTheDomainDefaultAsync | 3 | Three absent-reason variants use domain default. |

### JobService/Api/JobStateResponseTests — 2 methods / 2 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| TypedProjection_PreservesEveryLifecycleFieldAndTypedCheckpoint | 1 | Independently populated fields and same typed checkpoint. |
| TypedProjection_ExposesAnAbsentTypedCheckpoint | 1 | Missing checkpoint stays absent. |

### JobService/Api/RecurringJobExtensionsTests — 11 methods / 13 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| AddOrUpdateRecurringJobAsync_RequestCronPreservesTheCompleteCommandAsync | 1 | Request cron, job, properties, identity and token. |
| AddOrUpdateRecurringJobAsync_PublishConfiguratorPreservesScheduleAndPropertiesAsync | 1 | Configurator publication preserves complete recurrence. |
| RecurringConfigurationOverloads_ForwardEveryScheduleAndPropertyVariantAsync | 1 | Cron/configurator and properties variants forwarded. |
| ScheduleJobAsync_TypedPublishPreservesTheCompleteCommandAsync | 1 | Exact DueAt and typed one-shot command. |
| ScheduleJobFromValuesAsync_RequestInitializesTheTypedContractAsync | 1 | Initializer creates correct scheduled request. |
| ScheduledJobOverloads_ForwardEveryIdentityAndInitializerVariantAsync | 1 | Explicit/generated ID and typed/value variants. |
| AddOrUpdateRecurringJobAsync_RejectsAConfigurationWithoutRecurrenceAsync | 1 | Missing recurrence rejected. |
| AddOrUpdateRecurringJobAsync_RejectsMissingRequiredArgumentsAsync | 1 | Required job/values/configurator guards. |
| RecurringJobApis_RejectMissingNamesAsync | 3 | Missing/empty/whitespace names rejected. |
| ScheduledJobApis_RejectInvalidRequiredArgumentsAsync | 1 | Scheduled required arguments rejected. |
| RecurringCommands_UseTheSameDeterministicIdentityAsync | 1 | Command consistency paired with independent identity golden test. |

### JobService/Configuration/JobConsumerKindContractTests — 3 methods / 3 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Planning_ClaimsOnlyJobConsumersAndExposesCompleteEndpointMetadata | 1 | Actual planned Configure invocation, owner/count/metadata. |
| RuntimeAndBulkConfiguration_HonorOwnershipRegistrationAndExclusions | 1 | Actual direct/bulk configure and independent excluded counts. |
| DispatcherAndHarness_UseOnlyTheJobConsumerIdentity | 1 | Queue/dispatcher/harness ownership; JR-02 unexecuted callback. |

### JobService/Configuration/JobConsumerTimeProviderTests — 4 methods / 4 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Options_DefaultAndAssignedPathsHaveOneExplicitClockOwner | 1 | Default/assigned clock ownership. |
| DirectJobServiceOptions_PropagateAllLocalRuntimeSettings | 1 | Complete local option forwarding. |
| DirectJobServiceOptions_RejectNull | 1 | Exact missing options guard. |
| JobServiceRegistration_RejectsInvalidMaterializedRuntimeOptions | 1 | Invalid runtime options fail materialization. |

### JobService/Configuration/JobSagaPartitionKeyConfigurationTests — 2 methods / 2 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| UseJobSagaPartitionKeyFormatters_MapsEveryCoordinationContractToItsOwningIdentityAsync | 1 | All three coordinator families map exact owning IDs. |
| UseJobSagaPartitionKeyFormatters_RejectsANullBusConfigurator | 1 | Missing bus configurator rejected. |

### JobService/Configuration/JobServiceEndpointConfigurationTests — 7 methods / 7 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| ContainerlessConfiguration_AppliesTheOutboxWithoutInventingAScope | 1 | Containerless outbox with no fake container scope. |
| ExplicitOptionsConfiguration_UsesTheSuppliedInstanceAndCompleteOptionSet | 1 | Same options instance and complete settings. |
| ConfigureJobServiceEndpoints_RejectsMissingRequiredInputs | 1 | Required configuration input guards. |
| RegistrationContext_AppliesTheOutboxToEveryJobSagaEndpointAsync | 1 | Three endpoints probed for outbox/scope behavior. |
| DirectRegistrationContext_AddsScopeAndConfiguresEndpointsOnlyOnce | 1 | Actual direct configure counts and scope. |
| CoordinationAddresses_RejectUseBeforeEndpointConfiguration | 1 | Preconfiguration address access fails. |
| PartitionedReceiveMode_RejectsATransportWithoutTheCapability | 1 | Unsupported capability fails closed. |

### JobService/Configuration/JobServicePublicConfigurationApiTests — 6 methods / 6 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| AddJobService_AppliesOptionsAndExposesAFunctionalRegistrationFacade | 1 | Options applied and functional facade resolved. |
| JobServiceRegistrationFacade_ForwardsConfigurationAndRejectsNullCallbacks | 1 | Actual callback forwarding plus null guards. |
| AddJobSagaStateMachines_RegistersTheCompleteCoordinatorAndItsSharedProvider | 1 | All coordinator registrations and same provider. |
| TryAddJobDistributionStrategy_PreservesTheFirstScopedRegistration | 1 | First owner preserved with scoped lifetime. |
| IsJobServiceEndpoint_RecognizesDefinitionsConventionsAndInputBoundaries | 1 | Definition/convention identity and input boundaries. |
| DirectConfigurationHelpers_PreserveOwnershipAndRejectMissingInputs | 1 | Direct helper ownership and required-input guards. |

### JobService/Configuration/RecurringJobScheduleConfiguratorTests — 19 methods / 45 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| DailyAt_PreservesEveryTimeComponent | 2 | Exact supplied daily components. |
| OnDaysAt_UsesEveryDistinctSelectedDayAndTimeComponent | 1 | Distinct day set and clock components. |
| EveryHours_UsesTheIntervalAnchorTimeAndSelectedDays | 1 | Hour interval, anchor and selected days. |
| EveryMinutes_UsesTheIntervalAnchorTimeAndSelectedDays | 1 | Minute interval, anchor and selected days. |
| EverySeconds_UsesTheIntervalAnchorTimeAndSelectedDays | 1 | Second interval, anchor and selected days. |
| IntervalSchedule_DefaultsLeaveCalendarFieldsUnrestricted | 3 | Hour/minute/second defaults unrestricted. |
| CalendarPeriodSchedules_PreserveEverySuppliedComponent | 1 | Weekly/monthly/yearly component forwarding. |
| InTimeZone_StoresThePlatformTimeZoneIdentifier | 1 | Exact assigned timezone identifier. |
| IntervalOutsideCronRange_IsRejected | 6 | Both invalid edges of three intervals. |
| ClockComponentOutsideItsRange_IsRejected | 6 | Both invalid edges of three clock components. |
| DayOfMonthOutsideItsRange_IsRejected | 2 | Low/high day bounds. |
| MonthOutsideItsRange_IsRejected | 2 | Low/high month bounds. |
| YearlyOn_RejectsADayThatDoesNotExistInTheSelectedMonth | 3 | Invalid calendar dates rejected. |
| YearlyOn_AcceptsLeapDay | 1 | Leap-day recurrence accepted. |
| ScheduleValidation_RejectsAnEmptyTimeZoneIdentifier | 1 | Missing zone identity fails validation. |
| OnDaysAt_RequiresAtLeastOneDay | 1 | Empty day selection rejected. |
| UndefinedDayOfWeek_IsRejected | 2 | Undefined low/high day enum rejected. |
| EveryExtension_RejectsANullConfigurator | 9 | Nine required-configurator guards. |
| InTimeZone_RejectsANullTimeZone | 1 | Missing timezone rejected. |

### JobService/Integration/ContainerJobConsumerDiscoveryTests — 1 method / 1 case

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| DiscoveredJobConsumer_AcceptsAndExecutesThroughItsKebabServiceEndpointAsync | 1 | Actual discovered consumer accepts/executes payload 41 and typed completion. |

### JobService/Integration/InMemoryJobServiceTests — 14 methods / 14 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| PermanentFailure_PublishesTheSubmittedFaultedAndTypedFaultContractsAsync | 1 | Actual submitted/terminal typed faults. |
| ConfiguredJobRetry_CompletesOnTheSecondDistinctAttemptAsync | 1 | Distinct retry attempt and successful terminal lifecycle. |
| SavedCheckpoint_IsRestoredToTheNextAttemptAndTypedStateQueryAsync | 1 | Persisted checkpoint reaches next execution/query. |
| CancelRunningJob_ReachesTheConsumerAndClosesStatusAndSlotWithTheReasonAsync | 1 | Cancellation reason, consumer and released slot/status. |
| RetryAfterCancellation_UsesANewAttemptAndCompletesAsync | 1 | Canceled attempt replaced by new successful attempt. |
| CancelWaitingJob_PreservesTheRunningSlotAndPublishesItsWaitAndTerminalTransitionsAsync | 1 | Waiting cancellation leaves running owner intact. |
| CancelDuringSlotAllocation_PublishesCancellationAfterTheOutstandingResponseAsync | 1 | Held response cancellation ordering observed. |
| RegisteredStateMachines_CompleteTheLifecycleThroughTheScopedPublishFilterAsync | 1 | Actual scoped lifecycle publication. |
| ExplicitFinalization_RemovesACompletedJobWhenAutomaticFinalizationIsDisabledAsync | 1 | Explicit removal after preserved terminal state. |
| PublishingJobs_GeneratesDistinctNonEmptyIdentitiesAcrossEachLifecycleAsync | 1 | Multiple independently distinct/nonempty lifecycle IDs. |
| UnknownJobIdentity_ReturnsTheCompleteNotFoundStateAsync | 1 | Independent NotFound state contract. |
| RecurringJob_CancelReAddAndManualRunPreserveItsIdentityAndContinuationAsync | 1 | Cancel/re-add/manual continuity; barriers are prefix observations. |
| NamedRecurringJobs_KeepDistinctStableIdentitiesAcrossRunsUpdatesAndNoOpUpdatesAsync | 1 | Four stable distinct IDs across updates/no-ops. |
| OneShotJob_RunsAtTheProviderOwnedScheduledInstantAndThenCompletesAsync | 1 | Provider-clock one-shot completion; before-due proof remains adjacent. |

### JobService/JobService/ConsumeJobContextCancellationTests — 10 methods / 15 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| NotificationOperation_ForwardsTheExactCancellationTokenAsync | 6 | Exact tokens for all six arranged notification operations. |
| ReportProgress_ObservesAnAlreadyCanceledOperationTokenAsync | 1 | Exact pre-canceled token observed. |
| CancelJobHandle_ObservesCallerCancellationWhileWaitingForTheJobAsync | 1 | Caller cancellation interrupts waiting. |
| CancelJobHandle_ReleasesLocalOwnershipWhenExecutionIgnoresCancellationAsync | 1 | Virtual grace expiry releases handle; JR-03 local bound/cleanup. |
| StartJobAsync_SnapshotsTheCancellationDeadlineBeforeInvokingTheConsumerPipeAsync | 1 | One-minute captured deadline despite one-day mutation; JR-03. |
| Constructor_AcceptsAStartCommandWithoutJobPropertiesAsync | 1 | Optional missing job metadata accepted. |
| Constructor_SnapshotsAllMetadataWithoutExposingMutationCapabilitiesAsync | 1 | Source postmutation cannot alter captured read-only metadata. |
| ConcurrentFirstProgressReports_UseOneOrderedBufferAsync | 1 | 64 reports retain one sequence and complete independent contents. |
| NotifyStartedAsync_PublishesTheTypedJobPayloadAsync | 1 | Typed started payload forwarded. |
| NotificationMethods_RejectMissingRequiredValuesAsync | 1 | Required notification arguments rejected. |

### JobService/JobService/JobConsumerMessageFilterTests — 5 methods / 5 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| SuccessfulExecution_NotifiesTheCompleteLifecycleInOrderAsync | 1 | Exact started/run/completed trace and next-filter exclusion. |
| MatchingJobCancellation_IsReportedAsCanceledAsync | 1 | Matching cancellation reported as canceled. |
| RetryableFailure_ReportsTheNextPolicyDelayAsync | 1 | Exact next retry delay and original exception. |
| ExhaustedRetryPolicy_ReportsATerminalFaultAsync | 1 | Exhausted policy emits terminal null retry delay. |
| Filter_ValidatesRequiredInputsAndReportsItsInvocationSignatureAsync | 1 | Constructor/invocation guards and probe signature. |

### JobService/JobService/JobDistributionStrategyTests — 4 methods / 4 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| SelectInstance_SelectsTheLeastLoadedEligibleInstanceAsync | 1 | Independently arranged least-loaded eligible owner. |
| SelectInstance_PrefersTheLeastRecentlyUsedInstanceWhenLoadsMatchAsync | 1 | Exact recency tie-break owner. |
| SelectInstance_ReturnsNullWhenEveryInstanceIsAtItsLimitAsync | 1 | No eligible owner returns null. |
| SelectInstance_RejectsMissingInputsAndCancellationAsync | 1 | Missing input/canceled operation guards. |

### JobService/JobService/JobIdentityTests — 3 methods / 3 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| DeterministicIdentity_MatchesTheSha256GoldenVector | 1 | Independent fixed SHA256 identity vector. |
| DeterministicIdentity_IsStableAndNameSensitive | 1 | Same name stable, changed name differs. |
| IdentityFactories_RejectMissingNames | 1 | Required deterministic/recurring names rejected. |

### JobService/JobService/JobProgressBufferTests — 4 methods / 5 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| FlushDeadline_UsesTheProvidedTimeProviderAndPublishesTheLatestProgressAsync | 1 | Provider-owned flush deadline and latest progress. |
| Constructor_RejectsMissingNotificationContextAndTimeProvider | 1 | Exact required collaborators rejected. |
| Constructor_RejectsNonPositiveBufferLimits | 2 | Nonpositive count/time limits rejected. |
| Flush_PropagatesProgressPublicationFailureAsync | 1 | Publication fault escapes flush. |

### JobService/JobService/JobServiceBusObserverTests — 3 methods / 3 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Observer_StartsAfterReadinessAndStopsBeforeTheBusAsync | 1 | Start gated by readiness; stop ordered; JR-03 bound/cleanup. |
| Observer_PropagatesRuntimeAndReadinessFailuresAsync | 1 | Exact runtime/readiness failure propagation. |
| Observer_ValidatesEveryRequiredLifecycleInputAsync | 1 | Required lifecycle collaborator guards. |

### JobService/JobService/JobServiceLifecycleTests — 14 methods / 14 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| NewJobTypeSaga_InitializesEveryPersistedCollection | 1 | All required type-saga collections initialized. |
| NewJobSaga_InitializesRequiredPersistedDictionaries | 1 | Required job-saga dictionaries initialized. |
| StartAndStopTransitions_AreSerializedInBothDirectionsAsync | 1 | Held transitions prove both directions; JR-03 failure-safe gates. |
| Stop_CancelsAndDrainsTheExactHeartbeatGenerationBeforeReturningAsync | 1 | Exact active generation canceled/drained; finally cleanup present. |
| RepeatedStarts_CancelAndReplaceHeartbeatGenerationsWithoutOverlapAsync | 1 | Three generations never overlap; JR-03 held-gate cleanup. |
| HeartbeatPublicationFailure_DoesNotStopTheActiveGenerationAsync | 1 | Publication failure leaves active heartbeat generation. |
| FailedStart_LeavesNoHeartbeatAndDoesNotStrandTheLifecycleGateAsync | 1 | Failed startup leaves retryable lifecycle gate/no heartbeat. |
| RegisteredJobType_SnapshotsConcurrencyAndMetadataBeforeRuntimeStartsAsync | 1 | Postregistration option mutation cannot alter snapshot. |
| JobTypeRegistration_EnforcesRequiredInputsIdentityAndSingleOwnership | 1 | Type registration guards and single ownership. |
| Admission_FollowsTheCompleteSuccessfulAndFailedLifecycleSequenceAsync | 1 | Exact admission order across start/failure/stop/restart. |
| Stop_WaitsForAnAdmittedJobUntilItsHandleIsRegisteredAsync | 1 | Deliberate synchronous hold proves admission-drain race. |
| Stop_CancelsAndRemovesAnActiveLocalExecutionAsync | 1 | Original active execution canceled and handle removed. |
| ActiveJobIdentity_CannotBeAdmittedTwiceAsync | 1 | Exact conflict and original handle remain; JR-04 rejected work unobserved. |
| SynchronousPipelineFailure_ReleasesTheReservedIdentityAsync | 1 | Exact synchronous fault releases reservation for replacement. |

### JobService/JobService/StartJobConsumerTests — 4 methods / 4 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| MatchingCommand_DeserializesAndForwardsEveryAdmissionInputAsync | 1 | Same independently arranged serialized input and admission parameters. |
| ForeignJobType_IsIgnoredBeforeDeserializationAsync | 1 | Foreign type does zero deserialization/admission. |
| MissingDeserializedJob_FailsBeforeAdmissionAsync | 1 | Missing result rejected before admission. |
| Consumer_ValidatesEveryRequiredInputAsync | 1 | Runtime/consume required-input guards. |

### JobService/JobService/SuperviseJobConsumerTests — 5 methods / 8 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| CancelJobAttempt_IgnoresARequestForAnEarlierAttemptAsync | 1 | Stale attempt performs zero cancellation. |
| CancelJobAttempt_CancelsOnlyTheMatchingAttemptAsync | 1 | Matching attempt receives exact reason/token. |
| GetJobAttemptStatus_MapsEveryLocalTaskStateAsync | 4 | Four arranged task states map exact status/timestamp. |
| GetJobAttemptStatus_DoesNotAnswerForMissingOrStaleAttemptsAsync | 1 | Missing/stale handles receive no status answer. |
| Supervisor_RejectsMissingRuntimeAndConsumeContextsAsync | 1 | Required supervisor collaborators rejected. |

### JobService/Scheduling/CronExpressionCalendarTests — 4 methods / 23 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| DayOfMonthAndDayOfWeek_AreCombinedAsAUnion | 5 | Independent union predicate over declared dates. |
| JuneSchedule_MatchesTheCompleteExpectedCalendar | 5 | Complete independently arranged June schedules. |
| ThirdFridayHourRange_MatchesOnlyItsFourHalfHourSamples | 1 | Independent 200-sample predicate/exact four matches. |
| MonthAbbreviation_MapsToTheExpectedCalendarField | 12 | All twelve named months map exact numbers. |

### JobService/Scheduling/CronExpressionContractTests — 9 methods / 27 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| NullExpression_IsRejected | 1 | Missing expression rejected. |
| EquivalentExpressions_HaveStableTextEqualityAndHashCode | 8 | Independently declared equivalent expression pairs. |
| DefaultAndExplicitLocalTimeZones_HaveTheSameIdentity | 1 | Default/explicit local identity consistent. |
| EquivalentWhitespace_IsCanonicalizedForIdentity | 1 | Whitespace canonical identity. |
| NullTimeZone_IsRejected | 1 | Missing timezone rejected. |
| NullExpression_IsReportedAsInvalidByTheNonThrowingApi | 1 | Nonthrowing validator returns false. |
| ExpressionSummary_ReportsEveryParsedField | 1 | Summary contains every declared field. |
| ParsedField_ContainsAtLeastOneValue | 12 | Narrow nonempty field contract; exact-field tests separate. |
| IsSatisfiedBy_MatchesOnlyNamedInstants | 1 | Independent positive/negative named UTC instants. |

### JobService/Scheduling/CronExpressionDaylightSavingTests — 3 methods / 3 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| SpringGap_CarriesTheMissingOccurrenceIntoTheNextLocalHour | 1 | Custom-zone spring gap resolves exact UTC instant. |
| AutumnTransition_DateScheduleDoesNotFireOneHourEarly | 1 | Custom-zone date schedule exact standard offset. |
| AutumnTransition_WeekdayScheduleUsesTheStandardOffset | 1 | Custom-zone weekday exact posttransition UTC instant. |

### JobService/Scheduling/CronExpressionParsingTests — 21 methods / 136 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| LastDayLists_MatchEveryDeclaredDay | 8 | Exact independently expected last-day lists. |
| MultipleLastDayTokens_AreRejected | 1 | Multiple last-day tokens rejected. |
| LastToken_IsAcceptedOnlyInDayFields | 7 | Day-field-only last-token grammar. |
| NthWeekIndex_MustBeBetweenOneAndFive | 7 | Nth-week edge values rejected. |
| NthWeekdayIndex_MustBeBetweenOneAndSeven | 10 | Nth-weekday edge values rejected. |
| QuestionMark_AllowsOnlyTrailingWhitespace | 5 | Exact question-mark trailing grammar. |
| LastDayOffset_GreaterThanThirtyIsRejected | 1 | Excess last-day offset rejected. |
| NearestWeekday_GreaterThanThirtyOneIsRejected | 1 | Excess nearest-weekday value rejected. |
| LastWeekdayToken_CannotBeCombinedAsAList | 3 | Last-weekday/list ambiguity rejected. |
| AmbiguousFieldLists_AreRejectedWithTheirExactReason | 7 | Exact independently declared ambiguity reasons. |
| WraparoundRange_ContainsExactlyTheExpectedValues | 7 | Exact independent wraparound arrays. |
| NamedRangeSteps_ContainExactlyTheExpectedValues | 2 | Exact independent named-range step arrays. |
| NamedMonthStep_StartsAtTheDeclaredMonth | 1 | Named step preserves anchor month. |
| NamedLastWeekday_SelectsOnlyTheLastNamedWeekdayOfTheMonth | 1 | Independent last-weekday date selection. |
| InvalidNamedField_IsRejectedWithItsExactReason | 17 | Exact invalid named-field reasons. |
| InvalidIncrement_IsRejectedWithItsFieldSpecificReason | 34 | Exact invalid increment reasons by field. |
| FieldAfterOptionalYear_IsRejected | 1 | Extra postyear field rejected. |
| MalformedFields_AreRejectedAsFormatErrors | 7 | Malformed grammar yields domain format errors. |
| ValidationApi_RejectsGarbageAndAcceptsValidSteps | 13 | Independent invalid/valid validator inputs. |
| ExtraWhitespace_DoesNotChangeTheSchedule | 2 | Independently known schedule preserved by whitespace. |
| DayOfWeekOutsideItsRange_IsRejectedWithTheExactReason | 1 | Exact invalid weekday error. |

### JobService/Scheduling/CronExpressionSchedulingTests — 7 methods / 25 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| NextFireTime_MatchesThePublishedCalendarContract | 10 | Exact independently published next-fire UTC instants. |
| LastWeekdayOffset_IsClampedAtTheFirstDay | 6 | Independent first-day clamp dates. |
| NextFireTime_CrossesCalendarBoundaries | 5 | Exact month/year/calendar boundaries. |
| DayTwentyNine_SkipsFebruaryAndCrossesTheYear | 1 | Exact missing-day/year transition. |
| LastDayOffsets_MatchOnlyTheirCalculatedDates | 1 | Independent last-day date predicate. |
| NearestWeekday_NeverSchedulesAWeekend | 1 | Independently prohibited weekend results. |
| WeekdayExpression_CrossesTheYearWithoutLosingItsNextOccurrence | 1 | Exact crossyear weekday occurrence. |

### JobService/Scheduling/TimeZoneResolverTests — 4 methods / 6 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| SameUnknownIdentifier_CanBeResolvedDifferentlyByIndependentOwners | 1 | Two same-ID owner callbacks yield distinct exact zones/call counts. |
| SystemIdentifier_DoesNotInvokeTheOwnerResolver | 1 | System UTC bypasses owner callback. |
| UnknownIdentifierWithoutMatchingResolver_ThrowsTheDocumentedFailure | 1 | Exact domain error and inner timezone error. |
| MissingIdentifier_IsRejected | 3 | Missing/empty/whitespace ID guards. |

### JobService/Serialization/JobPropertyCollectionTests — 7 methods / 9 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Set_UsesCaseInsensitiveKeysAndExplicitMutationSemantics | 1 | Case-insensitive replacement/mutation contract. |
| Get_ReturnsTypedValuesAndDefaults | 1 | Typed extraction and independently expected defaults. |
| SetMany_RejectsInvalidKeysBeforeMutatingTheCollection | 1 | Bulk invalid keys fail before any partial mutation. |
| MutableCollection_PreservesTheCompleteDictionaryAndBulkContract | 1 | Full enumerated dictionary/bulk operations. |
| KeyedOperations_RejectMissingKeys | 3 | Missing/empty/whitespace keyed input guards. |
| JobOptions_ExposeTheMetadataContractWithoutTheStorageType | 1 | Options use interface not mutable implementation surface. |
| ReadOnlySnapshot_IsCaseInsensitiveCompleteAndNotMutationCapable | 1 | Complete independent keys/values and no mutation interface. |

### JobService/Serialization/JobPropertySnapshotTests — 1 method / 1 case

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Create_ReturnsAnIndependentCaseInsensitiveLastWriteWinsSnapshot | 1 | Independent source, case-insensitive last winner and detached mutation. |

### MessageJournal/MessageJournalContractTests — 8 methods / 14 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| StoreLimits_RejectEveryNonPositiveBoundary | 6 | Both nonpositive edges for three store limits. |
| StoreLimits_RejectInfiniteRetention | 1 | Infinite retention rejected. |
| Options_RejectNonPositiveWriteTimeout | 2 | Zero/negative timeout rejected. |
| Options_PreserveTheExplicitFiniteTimeoutAndClock | 1 | Same clock and exact finite timeout. |
| Options_RejectAMissingTimeProvider | 1 | Exact missing timeProvider guard. |
| Options_RejectAWriteTimeoutTheRuntimeTimerCannotRepresent | 1 | Unrepresentable timer delay rejected at composition. |
| Projection_DetachesEveryCallerOwnedInput | 1 | Independent body/types/metadata/headers survive source mutation. |
| Projection_RejectsAnUndefinedDataClassification | 1 | Undefined classification rejected. |

### MessageJournal/MessageJournalIntegrationTests — 7 methods / 7 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| WithoutExplicitConnection_TheJournalPerformsNoWorkAsync | 1 | Real delivery observed; JR-05 unreachable store oracle. |
| ExplicitOutgoingConnection_RecordsSendAndPublishTerminalEnvelopesExactlyOnceAsync | 1 | Actual connected send/publish envelope, scheduling/TTL/decimal metadata. |
| ConsumeJournal_RecordsTheExactTerminalSuccessAndFaultOutcomesAsync | 1 | Correlated terminal success/fault, failure type and expiration. |
| OutgoingJournal_RecordsTheExactSendAndPublishFaultOutcomesAsync | 1 | Same arranged send/publish failures and exact terminal records. |
| StoreFailure_DoesNotChangeASuccessfulMessageDeliveryAsync | 1 | Actual successful delivery, two failing append attempts/no entries. |
| InMemoryOutbox_RecordsTheDeferredMessageEnvelopeInsteadOfAnInternalWrapperAsync | 1 | Real deferred contract/body not serialized wrapper. |
| ConnectionHandle_DisconnectsEverySelectedJournalObserverAsync | 1 | Connected outgoing send then idempotent disconnect; all-selected priority separate. |

### MessageJournal/MessageJournalTelemetryTests — 4 methods / 4 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| StoredFilteredAndFailedWrites_EmitOnlyTheExactLowCardinalitySchemaAsync | 1 | Exact six measurements/three activities/schema and sensitive-tag exclusion. |
| Activity_SpansPolicyProjectionAndStorePersistenceAsync | 1 | Same active span across policy/store and exact parent/sample tags. |
| EveryFailurePath_EmitsExactlyOneStableReasonAsync | 1 | Eight exact sorted failure reasons incl fake-clock timeout. |
| ThrowingOpenTelemetryListeners_CannotChangeStorageOrEscapeTheWriterAsync | 1 | Counter/sample/stop listener faults preserve three writes. |

### MessageJournal/MessageJournalWriterTests — 13 methods / 13 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| Construction_RejectsMissingCollaboratorsAndUndeclaredStoreLimits | 1 | Exact store/policy/options/missing-limits guards. |
| Policy_IsTheOnlyBoundaryBetweenRawCaptureAndStoredEntryAsync | 1 | Independent sanitized body/classification/types/metadata/headers only. |
| NullProjection_FiltersTheObservationWithoutCallingTheStoreAsync | 1 | Actual connected store zero calls/entries. |
| OversizedSanitizedEntry_IsRejectedBeforePersistenceAsync | 1 | Body-size overflow rejected with zero append. |
| CapturePolicyAndStoreFailures_NeverEscapeTheJournalBoundaryAsync | 1 | Explicit three failures contained; policy zero writes/store one attempt. |
| CallerCancellation_IsObservedWithoutEscapingOrPersistingAsync | 1 | Store receives canceled operation and retains nothing. |
| ProviderTimeout_UsesTheConfiguredClockAndDoesNotWaitOnWallTimeAsync | 1 | Virtual one-minute timeout, cancellation observed, no entry. |
| StoredEntry_HasStableVersionSevenIdentityTimeAndDetachedContentAsync | 1 | UUID version/time/body independence; size lower bound only. |
| CaptureProjectionAndEntry_DoNotExposeMutableBodyBackingStorageAsync | 1 | Array extraction mutation cannot alter any retained body stage. |
| EscapedJsonContent_IsCountedBeforeTheStoreLimitIsAppliedAsync | 1 | Escaped metadata overflow rejects before append. |
| ContentSize_CountsEveryRetainedUtf8ValueAndJsonSeparatorAsync | 1 | Independent exact retained content size 166. |
| ThrowingTimeProvider_CannotEscapeTheJournalBoundaryAsync | 1 | Initial timestamp fault contained, zero append. |
| ThrowingElapsedClock_CannotEscapeAfterPersistenceAsync | 1 | Elapsed timestamp fault contained after one retained write. |

### ReliableMessaging/ReliableInMemoryIntegrationTests — 4 methods / 7 cases

| Method | Cases | Scoped observation |
| --- | ---: | --- |
| InboxLock_AllowsExactlyOneOfThreeConcurrentDeliveriesToPublishTheHundredEventsAsync | 1 | Drained harness, one execution, exact 100 unique single events. |
| ConsumerOutbox_PublishesBothScopedEventsExactlyOnceWithTheirRoutingKeysAsync | 2 | Success/first retry exact attempts, two events and alpha/beta routes after stop. |
| ConsumerInbox_RepeatedFailuresAdvanceAttemptsAndReachQuarantineAsync | 1 | Exact three attempts, quarantine and zero events/typed faults. |
| SagaOutbox_ReachesVerifiedWithOneCommittedStateMessageAsync | 3 | Success/consume retry/delivery failure each reach Verified, one committed message. |

Five standalone fixture files declare zero test methods/cases. They are included
in the 45-file manifest and full reading, not omitted from owner admission.

## Actual evidence and continuation

Historical native Core report:
`/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/final-core-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_17_43_15.966675.ctrf`;
SHA256 `69fdb2715a26518d9ead5d57ba55be7d433356527cbbdfda802c310f24802ea6`.
Its independently parsed summary and actual records are exactly 4,007 passed,
zero failures/skips/nonpassed records. The input Core tree and all selected bytes
are unchanged. Read-only enumeration terminates 0 and reconciles every personally
read [Fact]/[Theory] declaration to the exact historical native method and case
membership: **283 methods / 514 passed cases**, not a fresh iteration-124 run.
Receipt `reviewed-native-methods.log`, SHA256:
`ae5f6a15b92ef746dc0c67f2dff2bb6a8a6cac81c888b8d832564a4410b14cd9`.
The separate compatible manual-report checker terminates 0 and validates every
handwritten manifest row and all 283 method/case rows against current input
bytes and historical native records. Receipt SHA256:
`f23c76164c653024ecdb178dedc969988e124b307f8d7be7a10e4a41f87b2a45`.
Its initial text/binary regex encoding failure is preserved and causally corrected
in the reading packet, not relabeled a productive or native-test failure.

No new tests, productive source, runtime mutation, format repair, dependency or
gate change. No fresh broker/cloud/provider acceptance, package proof or current
whole-product coverage/CRAP is claimed. Current pure-reading/report changes do
not rerun unchanged strict builds or full Core/Architecture suites; exact file
bindings, all manual method/case rows, raw receipt hashes and owned diff whitespace
are independently checked instead. Historical passing cases do not close JR-01–06.

Next: complete remaining Core-owner reading and full parser/manifest admission,
then implement independent test corrections and connected productive contracts
with actual red/green and effective selected mutations. Preserve every feature
and all prior open findings. Full-src personal reading/manual comments,
greenfield API/type/file/folder/dummy/legacy/directive closure, metadata/package
baseline, real durable/provider acceptance and current global coverage/CRAP
remain required by the original active whole-product goal. Each checkpoint is
normally committed, annotated, pushed without force and independently ref-verified;
its security is not credited merely by a planned command or file text.
