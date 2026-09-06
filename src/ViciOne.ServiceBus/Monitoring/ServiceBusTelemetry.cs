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

    /// <summary>Defines the OpenTelemetry metric instrument names emitted by the service bus.</summary>
    public static class Metrics
    {
        /// <summary>Counts messages submitted by messaging clients.</summary>
        public const string SentMessages = "messaging.client.sent.messages";
        /// <summary>Counts messages received by messaging clients.</summary>
        public const string ConsumedMessages = "messaging.client.consumed.messages";
        /// <summary>Measures send and receive client-operation durations in seconds.</summary>
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
        /// <summary>Identifies the exception type for a failed operation.</summary>
        public const string ErrorType = "error.type";
        /// <summary>Distinguishes consumer and handler processors.</summary>
        public const string ProcessorKind = "vicione.servicebus.processor.kind";
        /// <summary>Identifies the outbox operation being observed.</summary>
        public const string OutboxOperation = "vicione.servicebus.outbox.operation";
        /// <summary>Records whether an operation succeeded or faulted.</summary>
        public const string Outcome = "vicione.servicebus.outcome";
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
