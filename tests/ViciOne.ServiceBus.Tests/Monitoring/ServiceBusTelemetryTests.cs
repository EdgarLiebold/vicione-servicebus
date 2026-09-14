using System.Diagnostics.Metrics;
using System.Reflection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

public sealed class ServiceBusTelemetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "one-stable-dotnet-telemetry-identity")]
    public void PublicTelemetryIdentity_IsOneStableDotnetSource()
    {
        Assert.Equal("ViciOne.ServiceBus", ServiceBusTelemetry.Name);
        Assert.Equal(ServiceBusTelemetry.Name, ServiceBusTelemetry.MeterName);
        Assert.Equal(ServiceBusTelemetry.Name, ServiceBusTelemetry.ActivitySourceName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-API", "fixed-schema-without-parallel-telemetry-api")]
    public void PublicApi_ExposesFixedOtelActivationAndNoParallelTelemetrySystem()
    {
        Assembly product = typeof(IBus).Assembly;
        MethodInfo useInstrumentation = Assert.Single(
            typeof(InstrumentationConfigurationExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == nameof(InstrumentationConfigurationExtensions.UseInstrumentation));

        ParameterInfo parameter = Assert.Single(useInstrumentation.GetParameters());
        Assert.Equal(typeof(IBusFactoryConfigurator), parameter.ParameterType);
        Assert.Equal(typeof(void), useInstrumentation.ReturnType);

        Assert.Null(product.GetType("ViciOne.ServiceBus.Monitoring.InstrumentationOptions"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.MetricsContext"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.MetricsContextExtensions"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.DependencyInjection.IHandlerConsumerAdapter"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.Logging.StartedInstrument"));
        Assert.False(typeof(MetricOperation).IsPublic);
        Assert.False(typeof(LogContextMetricsExtensions).IsPublic);
        Assert.DoesNotContain(product.GetExportedTypes(), type => type.Namespace is
            "ViciOne.ServiceBus.Logging.Diagnostics" or
            "ViciOne.ServiceBus.Logging.Internal" or
            "ViciOne.ServiceBus.Logging.Monitoring");
        Assert.DoesNotContain(product.GetExportedTypes(), type =>
            type.Namespace?.StartsWith("ViciOne.ServiceBus.Monitoring.Performance", StringComparison.Ordinal) == true
            || type.Name.Contains("StatsD", StringComparison.OrdinalIgnoreCase));

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "otel-messaging-instrument-identities")]
    public void MetricNames_UseTheApprovedOtelAndViciOneNamespaces()
    {
        string[] names =
        [
            ServiceBusTelemetry.Metrics.SentMessages,
            ServiceBusTelemetry.Metrics.ConsumedMessages,
            ServiceBusTelemetry.Metrics.ClientOperationDuration,
            ServiceBusTelemetry.Metrics.ProcessDuration,
            ServiceBusTelemetry.Metrics.ActiveOperations,
            ServiceBusTelemetry.Metrics.RetryAttempts,
            ServiceBusTelemetry.Metrics.DeliveryDuration,
            ServiceBusTelemetry.Metrics.OutboxMessages,
            ServiceBusTelemetry.Metrics.DurableSenderAdmission,
            ServiceBusTelemetry.Metrics.DurableSenderAdmissionSize,
            ServiceBusTelemetry.Metrics.DurableSenderDelivery,
            ServiceBusTelemetry.Metrics.DurableSenderDeliveryDuration,
            ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion,
            ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletionDuration,
            ServiceBusTelemetry.Metrics.ReliabilityAbandoned,
            ServiceBusTelemetry.Metrics.DurableSenderStored,
            ServiceBusTelemetry.Metrics.DurableSenderStoredContentSize,
            ServiceBusTelemetry.Metrics.DurableSenderPending,
            ServiceBusTelemetry.Metrics.DurableSenderRetryScheduled,
            ServiceBusTelemetry.Metrics.DurableSenderAwaitingConsumerCompletion,
            ServiceBusTelemetry.Metrics.DurableSenderQuarantined,
            ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge,
            ServiceBusTelemetry.Metrics.PayloadAdmission,
            ServiceBusTelemetry.Metrics.PayloadBodySize,
            ServiceBusTelemetry.Metrics.PayloadEnvelopeSize,
        ];

        Assert.Equal(
        [
            "messaging.client.sent.messages",
            "messaging.client.consumed.messages",
            "messaging.client.operation.duration",
            "messaging.process.duration",
            "vicione.servicebus.messaging.operations.active",
            "vicione.servicebus.messaging.retry.attempts",
            "vicione.servicebus.messaging.delivery.duration",
            "vicione.servicebus.outbox.messages",
            "vicione.servicebus.durable_sender.admission",
            "vicione.servicebus.durable_sender.admission.size",
            "vicione.servicebus.durable_sender.delivery",
            "vicione.servicebus.durable_sender.delivery.duration",
            "vicione.servicebus.durable_sender.consumer_completion",
            "vicione.servicebus.durable_sender.consumer_completion.duration",
            "vicione.servicebus.reliability.abandoned",
            "vicione.servicebus.durable_sender.stored",
            "vicione.servicebus.durable_sender.stored.content.size",
            "vicione.servicebus.durable_sender.pending",
            "vicione.servicebus.durable_sender.retry_scheduled",
            "vicione.servicebus.durable_sender.awaiting_consumer_completion",
            "vicione.servicebus.durable_sender.quarantined",
            "vicione.servicebus.durable_sender.oldest_pending.age",
            "vicione.servicebus.payload.admission",
            "vicione.servicebus.payload.body.size",
            "vicione.servicebus.payload.envelope.size",
        ], names);
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(names, name => Assert.True(
            name.StartsWith("messaging.", StringComparison.Ordinal)
            || name.StartsWith("vicione.servicebus.", StringComparison.Ordinal),
            $"Unexpected metric namespace: {name}"));

        string[] attributes =
        [
            ServiceBusTelemetry.Attributes.MessagingSystem,
            ServiceBusTelemetry.Attributes.OperationName,
            ServiceBusTelemetry.Attributes.OperationType,
            ServiceBusTelemetry.Attributes.DestinationName,
            ServiceBusTelemetry.Attributes.MessageBodySize,
            ServiceBusTelemetry.Attributes.ConversationId,
            ServiceBusTelemetry.Attributes.ErrorType,
            ServiceBusTelemetry.Attributes.ProcessorKind,
            ServiceBusTelemetry.Attributes.OutboxOperation,
            ServiceBusTelemetry.Attributes.Outcome,
            ServiceBusTelemetry.Attributes.Bus,
            ServiceBusTelemetry.Attributes.DurableSendId,
            ServiceBusTelemetry.Attributes.MessageContract,
            ServiceBusTelemetry.Attributes.DurableSenderRetainedContentSize,
            ServiceBusTelemetry.Attributes.DeliveryAttempt,
            ServiceBusTelemetry.Attributes.PayloadWarningThresholdExceeded,
            ServiceBusTelemetry.Attributes.ReliabilitySide,
            ServiceBusTelemetry.Attributes.MessageId,
            ServiceBusTelemetry.Attributes.CorrelationId,
            ServiceBusTelemetry.Attributes.InitiatorId,
            ServiceBusTelemetry.Attributes.RequestId,
            ServiceBusTelemetry.Attributes.SourceAddress,
            ServiceBusTelemetry.Attributes.DestinationAddress,
            ServiceBusTelemetry.Attributes.InputAddress,
            ServiceBusTelemetry.Attributes.MessageContracts,
            ServiceBusTelemetry.Attributes.ProcessorName,
            ServiceBusTelemetry.Attributes.CourierTrackingNumber,
            ServiceBusTelemetry.Attributes.SagaId,
            ServiceBusTelemetry.Attributes.SagaStateBefore,
            ServiceBusTelemetry.Attributes.SagaStateAfter,
            ServiceBusTelemetry.Attributes.RabbitMqRoutingKey,
            ServiceBusTelemetry.Attributes.ExceptionType,
            ServiceBusTelemetry.Attributes.ExceptionMessage,
            ServiceBusTelemetry.Attributes.ExceptionStackTrace,
        ];
        Assert.Equal(
        [
            "messaging.system",
            "messaging.operation.name",
            "messaging.operation.type",
            "messaging.destination.name",
            "messaging.message.body.size",
            "messaging.message.conversation_id",
            "error.type",
            "vicione.servicebus.processor.kind",
            "vicione.servicebus.outbox.operation",
            "vicione.servicebus.outcome",
            "vicione.servicebus.bus",
            "vicione.servicebus.durable_send.id",
            "vicione.servicebus.contract",
            "vicione.servicebus.durable_sender.retained_content.size",
            "vicione.servicebus.delivery.attempt",
            "vicione.servicebus.payload.warning_threshold_exceeded",
            "vicione.servicebus.reliability.side",
            "messaging.message.id",
            "vicione.servicebus.correlation.id",
            "vicione.servicebus.initiator.id",
            "vicione.servicebus.request.id",
            "vicione.servicebus.source.address",
            "vicione.servicebus.destination.address",
            "vicione.servicebus.input.address",
            "vicione.servicebus.contracts",
            "vicione.servicebus.processor.name",
            "vicione.servicebus.courier.tracking_number",
            "vicione.servicebus.saga.id",
            "vicione.servicebus.saga.state.before",
            "vicione.servicebus.saga.state.after",
            "messaging.rabbitmq.destination.routing_key",
            "exception.type",
            "exception.message",
            "exception.stacktrace",
        ], attributes);
        Assert.Equal(attributes.Length, attributes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(attributes, name => Assert.True(
            name.StartsWith("messaging.", StringComparison.Ordinal)
            || name.StartsWith("error.", StringComparison.Ordinal)
            || name.StartsWith("exception.", StringComparison.Ordinal)
            || name.StartsWith("vicione.servicebus.", StringComparison.Ordinal),
            $"Unexpected attribute namespace: {name}"));
        Assert.DoesNotContain(attributes, name => name == "messaging.operation"
            || name == "exception.escaped"
            || name.StartsWith("messaging.vicione-servicebus.", StringComparison.Ordinal));
        Assert.Equal("exception", ServiceBusTelemetry.Events.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-SCHEMA", "all-supported-transport-identities-normalize-to-bounded-values")]
    public void MessagingSystemNormalizer_MapsEverySupportedTransportIdentityToTheFixedSchema()
    {
        var expected = new Dictionary<string, string>
        {
            ["activemq"] = ServiceBusTelemetry.MessagingSystems.ActiveMq,
            ["amazon_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["amazonsqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws.sns"] = ServiceBusTelemetry.MessagingSystems.AmazonSns,
            ["aws-sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["aws_sqs"] = ServiceBusTelemetry.MessagingSystems.AmazonSqs,
            ["azure-service-bus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["db"] = ServiceBusTelemetry.MessagingSystems.Sql,
            ["eventhubs"] = ServiceBusTelemetry.MessagingSystems.AzureEventHubs,
            ["in-memory"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["loopback"] = ServiceBusTelemetry.MessagingSystems.InMemory,
            ["rabbitmq"] = ServiceBusTelemetry.MessagingSystems.RabbitMq,
            ["sb"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["servicebus"] = ServiceBusTelemetry.MessagingSystems.AzureServiceBus,
            ["sql"] = ServiceBusTelemetry.MessagingSystems.Sql,
            ["custom-transport"] = ServiceBusTelemetry.MessagingSystems.Other,
            [""] = ServiceBusTelemetry.MessagingSystems.Unknown,
        };

        Assert.All(expected, item =>
        {
            Assert.Equal(item.Value, MessagingSystemNormalizerTestDriver.Normalize(item.Key));
            Assert.Equal(item.Value, MessagingSystemNormalizerTestDriver.Normalize(item.Key.ToUpperInvariant()));
        });
        Assert.Equal(ServiceBusTelemetry.MessagingSystems.Unknown,
            MessagingSystemNormalizerTestDriver.Normalize(null));
    }
}
