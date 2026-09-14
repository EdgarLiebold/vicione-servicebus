namespace ViciOne.ServiceBus.Monitoring;

/// <summary>
/// Stable OpenTelemetry identities emitted by ViciOne.ServiceBus through the built-in .NET
/// <c>ActivitySource</c> and <c>Meter</c> APIs.
/// </summary>
public static class ServiceBusTelemetry
{
    /// <summary>The product-wide diagnostic source identity.</summary>
    public const string Name = "ViciOne.ServiceBus";
    /// <summary>The <see cref="System.Diagnostics.ActivitySource"/> name used for distributed traces.</summary>
    public const string ActivitySourceName = Name;
    /// <summary>The <see cref="System.Diagnostics.Metrics.Meter"/> name used for metrics.</summary>
    public const string MeterName = Name;

    /// <summary>Defines the low-cardinality names of service-bus activities.</summary>
    public static class Activities
    {
        /// <summary>Represents policy projection and optional persistence of one message-journal observation.</summary>
        public const string MessageJournalObserve = "message journal observe";
    }

    /// <summary>Defines the OpenTelemetry metric instrument names emitted by the service bus.</summary>
    public static class Metrics
    {
        /// <summary>Counts messages that producers attempted to send to a broker.</summary>
        public const string SentMessages = "messaging.client.sent.messages";
        /// <summary>Counts messages delivered to an application.</summary>
        public const string ConsumedMessages = "messaging.client.consumed.messages";
        /// <summary>Measures messaging client-operation durations in seconds.</summary>
        public const string ClientOperationDuration = "messaging.client.operation.duration";
        /// <summary>Measures consumer and handler processing durations in seconds.</summary>
        public const string ProcessDuration = "messaging.process.duration";
        /// <summary>Tracks messaging operations that are currently executing.</summary>
        public const string ActiveOperations = "vicione.servicebus.messaging.operations.active";
        /// <summary>Counts delivery attempts that execute after an initial failure.</summary>
        public const string RetryAttempts = "vicione.servicebus.messaging.retry.attempts";
        /// <summary>Measures elapsed time between message dispatch and processing.</summary>
        public const string DeliveryDuration = "vicione.servicebus.messaging.delivery.duration";
        /// <summary>Counts outbox enqueue and delivery outcomes.</summary>
        public const string OutboxMessages = "vicione.servicebus.outbox.messages";
        /// <summary>Counts durable-send admission outcomes.</summary>
        public const string DurableSenderAdmission = "vicione.servicebus.durable_sender.admission";
        /// <summary>Measures the logical retained content size of durable-send admission requests in bytes.</summary>
        public const string DurableSenderAdmissionSize = "vicione.servicebus.durable_sender.admission.size";
        /// <summary>Counts durable-send delivery-attempt outcomes.</summary>
        public const string DurableSenderDelivery = "vicione.servicebus.durable_sender.delivery";
        /// <summary>Measures durable-send delivery-attempt durations in seconds.</summary>
        public const string DurableSenderDeliveryDuration = "vicione.servicebus.durable_sender.delivery.duration";
        /// <summary>Counts process-local durable-send consumer-completion outcomes.</summary>
        public const string DurableSenderConsumerCompletion = "vicione.servicebus.durable_sender.consumer_completion";
        /// <summary>Measures process-local durable-send consumer-completion durations in seconds.</summary>
        public const string DurableSenderConsumerCompletionDuration = "vicione.servicebus.durable_sender.consumer_completion.duration";
        /// <summary>Counts explicit operator decisions to retain quarantined reliable messages as abandoned.</summary>
        public const string ReliabilityAbandoned = "vicione.servicebus.reliability.abandoned";
        /// <summary>Reports the most recently observed number of retained durable-send records.</summary>
        public const string DurableSenderStored = "vicione.servicebus.durable_sender.stored";
        /// <summary>Reports the most recently observed logical durable-send content size in bytes.</summary>
        public const string DurableSenderStoredContentSize = "vicione.servicebus.durable_sender.stored.content.size";
        /// <summary>Reports the most recently observed number of pending durable sends.</summary>
        public const string DurableSenderPending = "vicione.servicebus.durable_sender.pending";
        /// <summary>Reports the most recently observed number of durable sends awaiting retry.</summary>
        public const string DurableSenderRetryScheduled = "vicione.servicebus.durable_sender.retry_scheduled";
        /// <summary>Reports the most recently observed number of durable sends awaiting consumer completion.</summary>
        public const string DurableSenderAwaitingConsumerCompletion = "vicione.servicebus.durable_sender.awaiting_consumer_completion";
        /// <summary>Reports the most recently observed number of quarantined durable sends.</summary>
        public const string DurableSenderQuarantined = "vicione.servicebus.durable_sender.quarantined";
        /// <summary>Reports the age of the oldest pending durable send in seconds.</summary>
        public const string DurableSenderOldestPendingAge = "vicione.servicebus.durable_sender.oldest_pending.age";
        /// <summary>Counts serialized-payload admission outcomes.</summary>
        public const string PayloadAdmission = "vicione.servicebus.payload.admission";
        /// <summary>Measures serialized application-body sizes in bytes.</summary>
        public const string PayloadBodySize = "vicione.servicebus.payload.body.size";
        /// <summary>Measures final transport-envelope sizes in bytes.</summary>
        public const string PayloadEnvelopeSize = "vicione.servicebus.payload.envelope.size";
        /// <summary>Counts terminal message-journal observation results.</summary>
        public const string MessageJournalOperations = "vicione.servicebus.message_journal.operations";
        /// <summary>Measures message-journal observation durations in seconds.</summary>
        public const string MessageJournalDuration = "vicione.servicebus.message_journal.duration";
    }

    /// <summary>Defines the OpenTelemetry attribute names emitted by the service bus.</summary>
    public static class Attributes
    {
        /// <summary>Identifies the broker or transport implementation.</summary>
        public const string MessagingSystem = "messaging.system";
        /// <summary>Identifies the messaging operation.</summary>
        public const string OperationName = "messaging.operation.name";
        /// <summary>Classifies the operation as send, receive, or process.</summary>
        public const string OperationType = "messaging.operation.type";
        /// <summary>Identifies the destination presented to the messaging client.</summary>
        public const string DestinationName = "messaging.destination.name";
        /// <summary>Records the uncompressed application-body size in bytes.</summary>
        public const string MessageBodySize = "messaging.message.body.size";
        /// <summary>Identifies a conversation spanning multiple related messages.</summary>
        public const string ConversationId = "messaging.message.conversation_id";
        /// <summary>Identifies the exception type for a failed operation.</summary>
        public const string ErrorType = "error.type";
        /// <summary>Distinguishes consumer and handler processors.</summary>
        public const string ProcessorKind = "vicione.servicebus.processor.kind";
        /// <summary>Identifies the outbox operation being observed.</summary>
        public const string OutboxOperation = "vicione.servicebus.outbox.operation";
        /// <summary>Records whether an operation succeeded or faulted.</summary>
        public const string Outcome = "vicione.servicebus.outcome";
        /// <summary>Identifies the typed bus that owns an observation.</summary>
        public const string Bus = "vicione.servicebus.bus";
        /// <summary>Identifies a durable-send intent without exposing its payload.</summary>
        public const string DurableSendId = "vicione.servicebus.durable_send.id";
        /// <summary>Identifies the stable message contract associated with an observation.</summary>
        public const string MessageContract = "vicione.servicebus.contract";
        /// <summary>Records the logical retained content size of a durable send in bytes.</summary>
        public const string DurableSenderRetainedContentSize = "vicione.servicebus.durable_sender.retained_content.size";
        /// <summary>Records the one-based durable-send delivery attempt number.</summary>
        public const string DeliveryAttempt = "vicione.servicebus.delivery.attempt";
        /// <summary>Indicates whether the payload warning threshold was exceeded.</summary>
        public const string PayloadWarningThresholdExceeded = "vicione.servicebus.payload.warning_threshold_exceeded";
        /// <summary>Identifies whether a reliable-messaging observation applies to the inbox or outbox.</summary>
        public const string ReliabilitySide = "vicione.servicebus.reliability.side";
        /// <summary>Identifies an individual message using the OpenTelemetry semantic convention.</summary>
        public const string MessageId = "messaging.message.id";
        /// <summary>Identifies the application correlation chain associated with a message.</summary>
        public const string CorrelationId = "vicione.servicebus.correlation.id";
        /// <summary>Identifies the message that initiated the current conversation.</summary>
        public const string InitiatorId = "vicione.servicebus.initiator.id";
        /// <summary>Identifies a request/response interaction.</summary>
        public const string RequestId = "vicione.servicebus.request.id";
        /// <summary>Records the logical source address carried by the message envelope.</summary>
        public const string SourceAddress = "vicione.servicebus.source.address";
        /// <summary>Records the logical destination address carried by the message envelope.</summary>
        public const string DestinationAddress = "vicione.servicebus.destination.address";
        /// <summary>Records the receive endpoint address that accepted a message.</summary>
        public const string InputAddress = "vicione.servicebus.input.address";
        /// <summary>Lists the contracts supported by a serialized message.</summary>
        public const string MessageContracts = "vicione.servicebus.contracts";
        /// <summary>Identifies the consumer, handler, saga, or activity processing a message.</summary>
        public const string ProcessorName = "vicione.servicebus.processor.name";
        /// <summary>Identifies a Courier routing slip.</summary>
        public const string CourierTrackingNumber = "vicione.servicebus.courier.tracking_number";
        /// <summary>Identifies the saga instance processing a message.</summary>
        public const string SagaId = "vicione.servicebus.saga.id";
        /// <summary>Records the saga state before message processing.</summary>
        public const string SagaStateBefore = "vicione.servicebus.saga.state.before";
        /// <summary>Records the saga state after message processing.</summary>
        public const string SagaStateAfter = "vicione.servicebus.saga.state.after";
        /// <summary>Records the RabbitMQ routing key selected for a send.</summary>
        public const string RabbitMqRoutingKey = "messaging.rabbitmq.destination.routing_key";
        /// <summary>Identifies the exception type recorded by an exception event.</summary>
        public const string ExceptionType = "exception.type";
        /// <summary>Records the exception message attached to an exception event.</summary>
        public const string ExceptionMessage = "exception.message";
        /// <summary>Records the exception stack trace attached to an exception event.</summary>
        public const string ExceptionStackTrace = "exception.stacktrace";
        /// <summary>Identifies whether the journal observed a send, publish, or consume operation.</summary>
        public const string MessageJournalOperation = "vicione.servicebus.message_journal.operation";
        /// <summary>Records the terminal message outcome presented to the journal.</summary>
        public const string MessageJournalOutcome = "vicione.servicebus.message_journal.outcome";
        /// <summary>Records whether a journal observation was stored, filtered, or failed.</summary>
        public const string MessageJournalResult = "vicione.servicebus.message_journal.result";
        /// <summary>Identifies the bounded processing phase responsible for a failed journal observation.</summary>
        public const string MessageJournalFailureReason = "vicione.servicebus.message_journal.failure.reason";
    }

    /// <summary>Defines the names of events emitted on service-bus activities.</summary>
    public static class Events
    {
        /// <summary>Identifies an exception event.</summary>
        public const string Exception = "exception";
    }

    /// <summary>Defines the OpenTelemetry messaging-system identifiers used by supported transports.</summary>
    public static class MessagingSystems
    {
        /// <summary>The OpenTelemetry identifier for Apache ActiveMQ transports.</summary>
        public const string ActiveMq = "activemq";
        /// <summary>The OpenTelemetry identifier for Amazon Simple Notification Service.</summary>
        public const string AmazonSns = "aws.sns";
        /// <summary>The OpenTelemetry identifier for Amazon Simple Queue Service.</summary>
        public const string AmazonSqs = "aws_sqs";
        /// <summary>The bounded identifier used for Azure Event Hubs transports.</summary>
        public const string AzureEventHubs = "eventhubs";
        /// <summary>The bounded identifier used for Azure Service Bus transports.</summary>
        public const string AzureServiceBus = "servicebus";
        /// <summary>The bounded identifier used for the in-memory transport.</summary>
        public const string InMemory = "in-memory";
        /// <summary>The OpenTelemetry identifier for RabbitMQ transports.</summary>
        public const string RabbitMq = "rabbitmq";
        /// <summary>The bounded identifier shared by SQL-backed transports.</summary>
        public const string Sql = "sql";
        /// <summary>The bounded identifier for a nonempty unrecognized transport.</summary>
        public const string Other = "other";
        /// <summary>The bounded identifier used when no transport identity is available.</summary>
        public const string Unknown = "unknown";
    }
}
