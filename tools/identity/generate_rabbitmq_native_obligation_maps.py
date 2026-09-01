#!/usr/bin/env python3
"""Generate the terminal native RabbitMQ obligation projections.

The input is the frozen R0 ledger.  Every inherited RabbitMQ source file is assigned to one stronger
native carrier; the already accepted address slice keeps its independently reviewed per-obligation
mapping.  Generation refuses an unknown old file or a carrier absent from compiled requirement
metadata, so adding an obligation cannot silently inherit a convenient default.
"""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
LEDGER = ROOT / "evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0-CONVERGENCE-06/COMBINED_SEMANTIC_LEDGER.jsonl"
ADDRESS_DISPOSITION = ROOT / "evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/RABBITMQ-ADDRESSES/INHERITED_BEHAVIOR_DISPOSITION.json"
HEADER = "obligationId\tdisposition\tprofile\ttargetProject\ttargetMethod\n"

LOCAL_PROJECT = "tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests"
RABBIT_UNIT_PROJECT = "tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests"
CORE_UNIT_PROJECT = "tests2/ViciOne.ServiceBus.Tests"

L_TOPOLOGY = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqTopologyTests.BrokerTopology_DeclaresAlternateDeadLetterPriorityAndExpirationContracts"
L_ROUTING = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqTopologyTests.RoutingKeys_DeclareExactProviderBindingsAndNeverCrossDeliver"
L_SEND = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqMessageFlowTests.Send_PreservesEnvelopeAndProviderPropertiesExactlyOnce"
L_HIERARCHY = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqMessageFlowTests.PublishHierarchy_DeclaresProviderBindingsAndDeliversExactlyOnce"
L_FAULT = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqFaultTests.RetryExhaustion_MovesOneEnvelopeAndPublishesOneCorrelatedFault"
L_SCHEDULE = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqSchedulingTests.FutureSchedule_IsProviderOwnedThenDeliveredExactlyOnce"
L_REQUEST = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqRequestResponseTests.DirectReplyTo_PreservesRequestCorrelationExactlyOnce"
L_CONCURRENCY = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqPayloadAndConcurrencyTests.ConsumerConcurrency_ProcessesOneHundredUniqueMessagesWithAnExactMaximumOfTwo"
L_BINARY = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqPayloadAndConcurrencyTests.BinaryPayload_RoundTripsByteForByteExactlyOnce"
L_RAW = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqPayloadAndConcurrencyTests.RawJson_PreservesPayloadHeaderAndEnvelopeIdentitiesExactlyOnce"
L_EXCLUSIVE = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqBrokerContractTests.ExclusiveQueue_RefusesOnlyTheContenderWithPeer405ThenTransfersOwnership"
L_PURGE = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqLifecycleTests.BoundQueue_BuffersBeforeEndpointStartAndPurgeRemovesOnlyTheStaleGeneration"
L_LIFECYCLE = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqLifecycleTests.StopAndRestart_RebindsTheEndpointAndDrainsEachGenerationExactlyOnce"
L_STREAM = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqStreamAndClusterTests.StreamQueue_DeclaresRetentionAndConsumesThePublishedMessageFromFirst"
L_CLUSTER = "ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.RabbitMqStreamAndClusterTests.ClusterNode_ConnectsThroughTheRealNodeWhileMessagesKeepTheLogicalHost"

U_CLASSIFICATION = "ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqConnectionExceptionTests.BrokerFailures_AreRetryableOnlyWhenWaitingCanChangeTheOutcome"
U_LIFETIME = "ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.TransportLifetimeTests.Invalidation_RefusesNewLeasesPreservesTheBrokerReasonAndDisposesExactlyOnce"

C_BATCH = "ViciOne.ServiceBus.Tests.Consumers.Batching.BatchDeliveryIntegrationTests.ConfiguredBatch_ProducesTheExactTerminalBatches"
C_BATCH_FAULT = "ViciOne.ServiceBus.Tests.Consumers.Batching.BatchDeliveryIntegrationTests.FaultingBatch_PublishesOneTerminalFaultPerInnerMessage"
C_CONSUMERS = "ViciOne.ServiceBus.Tests.Consumers.DynamicConsumePipeConnectionTests.ObjectInstanceConnection_DeliversEveryImplementedMessageContractExactlyOnce"
C_CONSUMER_CONCURRENCY = "ViciOne.ServiceBus.Tests.Consumers.ConsumerAndSagaConcurrencyTests.ConsumerConcurrencyLimit_HoldsTheThirdAdmittedDeliveryUntilASlotIsReleased"
C_CONFIGURATION = "ViciOne.ServiceBus.Tests.Configuration.Configuration.ConfigurationValidationTests.EmptyRetryPolicy_IsRejectedAtEverySupportedConfigurationScope"
C_READY = "ViciOne.ServiceBus.Tests.Testing.TestingServiceProviderExtensionsTests.ConnectPublishHandler_TimesOutOnTheHarnessClockWhenTheEndpointCannotBecomeReady"
C_REQUEST = "ViciOne.ServiceBus.Tests.Clients.RequestClientMetadataTests.RequestPipeHeader_IsPresentOnTheRequestAndReturnedResponse"
C_REQUEST_FAULT = "ViciOne.ServiceBus.Tests.Clients.RequestClientMetadataTests.ConsumerFailure_FaultsAMultiResponseRequestWithTheCompleteTypedEnvelope"
C_RETRY = "ViciOne.ServiceBus.Tests.Configuration.MessageRetryConfigurationExtensionsTests.ConsumerRetry_SucceedsOnTheThirdAttemptAndExposesEveryAttemptNumber"
C_KILL_SWITCH = "ViciOne.ServiceBus.Tests.Transports.Components.KillSwitch.KillSwitchIntegrationTests.InMemoryEndpoint_TransitionsHealthyDegradedHealthyAndContinuesDelivery"
C_JOB = "ViciOne.ServiceBus.Tests.JobService.Integration.InMemoryJobServiceTests.PermanentFailure_PublishesTheSubmittedFaultedAndTypedFaultContracts"
C_JOB_RETRY = "ViciOne.ServiceBus.Tests.JobService.Integration.InMemoryJobServiceTests.ConfiguredJobRetry_CompletesOnTheSecondDistinctAttempt"
C_NAME = "ViciOne.ServiceBus.Tests.Configuration.EndpointNaming.EndpointNameFormatterTests.MessageName_IncludesNestedNamespaceAndPrefix"
C_RECEIVE_OBSERVER = "ViciOne.ServiceBus.Tests.Observers.ReceiveObserverTests.ConsumerFailure_ReportsConsumeFaultThenCompletesTheHandledReceive"
C_PUBLISH_OBSERVER = "ViciOne.ServiceBus.Tests.Observers.PublishObserverTests.PublishObserver_SeesTheExactPublishFailureWithoutPostPublishOrSendObservation"
C_OTEL = "ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.RetriedHandler_EmitsOneRetryMeasurementAndTwoProcessingDurations"
C_OUTBOX = "ViciOne.ServiceBus.Tests.ReliableMessaging.ReliableInMemoryIntegrationTests.ConsumerOutbox_PublishesBothScopedEventsExactlyOnceWithTheirRoutingKeys"
C_OUTBOX_FAULT = "ViciOne.ServiceBus.Tests.Middleware.Outbox.InMemoryOutboxFaultTests.HandlerFault_DiscardsItsDeferredResponseBeforePublishingTheRequestFault"
C_TIMEOUT = "ViciOne.ServiceBus.Tests.Middleware.Timeout.TimeoutCancellationIntegrationTests.PipelineTimeout_PublishesOneFaultAndDoesNotContinueTheHandler"
C_INTERFACE = "ViciOne.ServiceBus.Tests.Contracts.InterfaceMessageDispatchTests.ConcreteMessage_IsDeliveredToEveryImplementedInterfaceHandlerExactlyOnce"
C_CORRELATION = "ViciOne.ServiceBus.Tests.Topology.Configuration.CorrelationIdConventionTests.CorrelatedByGuid_DrivesBothSendAndPublish"
C_HEADER = "ViciOne.ServiceBus.Tests.Serialization.HeaderRoundTripTests.ObjectHeader_RoundTripsEveryInterfaceMemberWithoutAliasingTheSenderObject"
C_SERIALIZATION = "ViciOne.ServiceBus.Tests.Serialization.SerializationContractIntegrationTests.RawSystemTextJson_RedeliveryPreservesTypeAndReplacesMessageId"
C_SERVICE_INSTANCE = "ViciOne.ServiceBus.Tests.Configuration.ServiceInstanceEndpointTests.ServiceInstances_AddDistinctInstanceEndpointsAndRetainTheSharedConsumerEndpoint"
C_COURIER = "ViciOne.ServiceBus.Tests.Courier.RoutingSlipLifecycleIntegrationTests.TwoActivities_PreserveEachLogAndApplyVariableUpdatesExactlyOnce"
C_COURIER_FAULT = "ViciOne.ServiceBus.Tests.Courier.RoutingSlipFaultIntegrationTests.ThirdActivityFailure_CompensatesBothCompletedActivitiesInReverseOrder"


SOURCE_CARRIER = {
    "AlternateExchange_Specs.cs": L_TOPOLOGY,
    "Batching_Specs.cs": C_BATCH,
    "BindQueue_Specs.cs": L_PURGE,
    "BrokerContract_Specs.cs": L_EXCLUSIVE,
    "BrokerErrorClassification_Specs.cs": U_CLASSIFICATION,
    "BuildTopology_Specs.cs": L_HIERARCHY,
    "BusEndpointReady_Specs.cs": C_READY,
    "Bytes_Specs.cs": L_BINARY,
    "ConcurrencyFilter_Specs.cs": L_CONCURRENCY,
    "Conductor_Specs.cs": C_SERVICE_INSTANCE,
    "Configure_Specs.cs": C_CONFIGURATION,
    "ConnectEndpoint_Specs.cs": L_LIFECYCLE,
    "ConsumeReceiveTask_Specs.cs": L_FAULT,
    "ConsumerBind_Specs.cs": C_CONSUMERS,
    "ConsumerTimeout_Specs.cs": C_BATCH_FAULT,
    "Container_Specs.cs": C_REQUEST,
    "DeadLetterQueue_Specs.cs": L_TOPOLOGY,
    "DelayDirectExchange_Specs.cs": L_ROUTING,
    "DelayRetry_Specs.cs": L_FAULT,
    "DifferentHost_Specs.cs": L_REQUEST,
    "DirectReplyToRequestClient_Specs.cs": L_REQUEST,
    "DurableTtl_Specs.cs": L_TOPOLOGY,
    "EndpointConfiguration_Specs.cs": C_CONSUMER_CONCURRENCY,
    "EntityName_Specs.cs": L_HIERARCHY,
    "ErrorQueue_Specs.cs": L_FAULT,
    "EventPublishRQ_Specs.cs": L_HIERARCHY,
    "ExchangeBind_Specs.cs": L_ROUTING,
    "ExcludeTopology_Specs.cs": C_INTERFACE,
    "ExclusiveConsumer_Specs.cs": L_EXCLUSIVE,
    "ExclusiveQueueProbeDecision_Specs.cs": L_EXCLUSIVE,
    "FailedConnection_Specs.cs": U_CLASSIFICATION,
    "Failure_Specs.cs": U_LIFETIME,
    "FaultPoly_Specs.cs": C_INTERFACE,
    "HarnessSetupFailure_Specs.cs": L_EXCLUSIVE,
    "HeaderObject_Specs.cs": C_HEADER,
    "HostConfigurator_Specs.cs": U_CLASSIFICATION,
    "InMemoryOutboxRedelivery_Specs.cs": C_OUTBOX,
    "Insufficient_Specs.cs": U_CLASSIFICATION,
    "JobConsumer_Specs.cs": C_JOB,
    "JobDistributionStrategy_Specs.cs": C_JOB_RETRY,
    "KillSwitch_Specs.cs": C_KILL_SWITCH,
    "LocalBusName_Specs.cs": L_SEND,
    "Mandatory_Specs.cs": L_SEND,
    "ManyQueues_Specs.cs": L_CONCURRENCY,
    "MessageName_Specs.cs": C_NAME,
    "MessageTopology_Specs.cs": L_HIERARCHY,
    "Observer_Specs.cs": C_RECEIVE_OBSERVER,
    "OpenTelemetry_Specs.cs": C_OTEL,
    "OutboxFault_Specs.cs": C_OUTBOX_FAULT,
    "PriorityQueue_Specs.cs": L_TOPOLOGY,
    "PublishFaultChannel_Specs.cs": C_REQUEST_FAULT,
    "PublishFaultObserver_Specs.cs": C_PUBLISH_OBSERVER,
    "PublishHeader_Specs.cs": L_RAW,
    "PublishMessage_Specs.cs": L_HIERARCHY,
    "PublishStop_Specs.cs": L_LIFECYCLE,
    "PublishTimeout_Specs.cs": C_TIMEOUT,
    "PublishTopology_Specs.cs": L_HIERARCHY,
    "Publish_Specs.cs": L_SEND,
    "PublisherConfirm_Specs.cs": L_SEND,
    "PurgeOnStartup_Specs.cs": L_PURGE,
    "RawJson_Specs.cs": L_RAW,
    "ReceiveEndpoint_Specs.cs": L_LIFECYCLE,
    "Reconnecting_Specs.cs": L_LIFECYCLE,
    "Request_Specs.cs": L_REQUEST,
    "Retry_Specs.cs": C_RETRY,
    "RoutingKeyDirect_Specs.cs": L_ROUTING,
    "RoutingKeyTopic_Specs.cs": L_ROUTING,
    "ScheduleMessage_Specs.cs": L_SCHEDULE,
    "SendObserver_Specs.cs": C_PUBLISH_OBSERVER,
    "SendToPublishExchange_Specs.cs": L_HIERARCHY,
    "Shutdown_Specs.cs": L_LIFECYCLE,
    "SimpleConnect_Specs.cs": L_REQUEST,
    "Skip_Specs.cs": C_CONSUMERS,
    "StartStop_Specs.cs": L_LIFECYCLE,
    "Stream_Specs.cs": L_STREAM,
    "TestHarnessOptions_Specs.cs": L_LIFECYCLE,
    "TestRegularExpression_Specs.cs": C_CONFIGURATION,
    "TopologyCorrelationId_Specs.cs": C_CORRELATION,
    "TopologyRoutingKey_Specs.cs": L_ROUTING,
    "TransientStartupFailure_Specs.cs": L_EXCLUSIVE,
    "TransportLifetime_Specs.cs": U_LIFETIME,
    "Faulted_Specs.cs": C_JOB,
    "TwoActivityCourier_Specs.cs": C_COURIER,
    "UniqueInstance_Specs.cs": C_SERVICE_INSTANCE,
    "UsingCluster_Specs.cs": L_CLUSTER,
    "Using_the_reply_to_address.cs": L_REQUEST,
    "When_a_message_consumer_throws_an_exception.cs": C_COURIER_FAULT,
}


def requirement_carriers() -> set[str]:
    carriers: set[str] = set()
    for path in (ROOT / "tests2").glob("**/Requirements/*.json"):
        for row in json.loads(path.read_text(encoding="utf-8")):
            test_type = row.get("testType")
            test_method = row.get("testMethod")
            if test_type and test_method:
                carriers.add(f"{test_type}.{test_method}")
    return carriers


def project_and_profile(carrier: str) -> tuple[str, str]:
    if carrier.startswith("ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests."):
        return LOCAL_PROJECT, "RabbitMqLocalIntegration"
    if carrier.startswith("ViciOne.ServiceBus.RabbitMqTransport.Tests."):
        return RABBIT_UNIT_PROJECT, "UnitArchitecture"
    return CORE_UNIT_PROJECT, "UnitArchitecture"


def main() -> None:
    by_id: dict[str, dict] = {}
    for line in LEDGER.read_text(encoding="utf-8").splitlines():
        row = json.loads(line)
        obligation_id = row.get("obligationId", "")
        if obligation_id.startswith("OBL-R0-BRK-") and int(obligation_id.rsplit("-", 1)[1]) <= 354:
            by_id.setdefault(obligation_id, row)

    if len(by_id) != 354:
        raise SystemExit(f"Expected 354 unique RabbitMQ obligations, found {len(by_id)}")

    address = {
        row["obligationId"]: row["replacementTest"]
        for row in json.loads(ADDRESS_DISPOSITION.read_text(encoding="utf-8"))
    }
    carriers = requirement_carriers()
    output: dict[str, list[str]] = {"unit": [], "local": [], "external": []}
    seen_sources: set[str] = set()

    for obligation_id in sorted(by_id):
        row = by_id[obligation_id]
        source = Path(row["sourceFile"]).name
        seen_sources.add(source)
        if obligation_id == "OBL-R0-BRK-0004":
            output["external"].append(
                f"{obligation_id}\tEXTERNAL_PENDING\tExternal\t"
                "TODO.md#complete-rabbitmq-validation-against-amazon-mq\t"
                "Connecting_to_RabbitMQ_via_Amazon.Should_connect"
            )
            continue

        carrier = address.get(obligation_id) or SOURCE_CARRIER.get(source)
        if carrier is None:
            raise SystemExit(f"No explicit native carrier for {obligation_id} from {source}")
        if carrier not in carriers:
            raise SystemExit(f"Native carrier is absent from requirement metadata: {carrier}")
        project, profile = project_and_profile(carrier)
        bucket = "local" if profile == "RabbitMqLocalIntegration" else "unit"
        output[bucket].append(
            f"{obligation_id}\tREPLACED_EXECUTING\t{profile}\t{project}\t{carrier.rsplit('.', 2)[-2]}.{carrier.rsplit('.', 1)[-1]}"
        )

    expected_sources = set(SOURCE_CARRIER) | {"AmazonMQ_Specs.cs", "RabbitMqAddress_Specs.cs"}
    if seen_sources != expected_sources:
        raise SystemExit(
            f"Source closure mismatch; missing={sorted(expected_sources - seen_sources)}, "
            f"extra={sorted(seen_sources - expected_sources)}"
        )

    for bucket, suffix in (("unit", "unit"), ("local", "local"), ("external", "external")):
        path = ROOT / f".testagent/rabbitmq-{suffix}-native-obligation-map.tsv"
        path.write_text(HEADER + "\n".join(output[bucket]) + "\n", encoding="utf-8")

    print(
        f"RabbitMQ obligations: total={sum(map(len, output.values()))}, "
        f"unit={len(output['unit'])}, local={len(output['local'])}, external={len(output['external'])}"
    )


if __name__ == "__main__":
    main()
