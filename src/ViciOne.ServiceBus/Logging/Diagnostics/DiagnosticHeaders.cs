namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides a diagnostic headers implementation.
/// </summary>
public static class DiagnosticHeaders
{
    /// <summary>
    /// Defines the default listener name value.
    /// </summary>
    public const string DefaultListenerName = Monitoring.ServiceBusTelemetry.ActivitySourceName;

    /// <summary>
    /// Defines the diagnostic id value.
    /// </summary>
    public const string DiagnosticId = "Diagnostic-Id";
    /// <summary>
    /// Defines the activity id value.
    /// </summary>
    public const string ActivityId = MessageHeaders.Prefix + "Activity-Id";
    /// <summary>
    /// Defines the activity correlation context value.
    /// </summary>
    public const string ActivityCorrelationContext = MessageHeaders.Prefix + "Activity-Correlation-Context";
    /// <summary>
    /// Defines the activity trace state value.
    /// </summary>
    public const string ActivityTraceState = MessageHeaders.Prefix + "Activity-Trace-State";
    /// <summary>
    /// Defines the activity propagation value.
    /// </summary>
    public const string ActivityPropagation = MessageHeaders.Prefix + "Activity-Propagation";

    /// <summary>
    /// Defines the message id value.
    /// </summary>
    public const string MessageId = "messaging.vicione-servicebus.message_id";
    /// <summary>
    /// Defines the correlation id value.
    /// </summary>
    public const string CorrelationId = "messaging.vicione-servicebus.correlation_id";
    /// <summary>
    /// Defines the initiator id value.
    /// </summary>
    public const string InitiatorId = "messaging.vicione-servicebus.initiator_id";
    /// <summary>
    /// Defines the request id value.
    /// </summary>
    public const string RequestId = "messaging.vicione-servicebus.request_id";
    /// <summary>
    /// Defines the source address value.
    /// </summary>
    public const string SourceAddress = "messaging.vicione-servicebus.source_address";
    /// <summary>
    /// Defines the destination address value.
    /// </summary>
    public const string DestinationAddress = "messaging.vicione-servicebus.destination_address";
    /// <summary>
    /// Defines the input address value.
    /// </summary>
    public const string InputAddress = "messaging.vicione-servicebus.input_address";
    /// <summary>
    /// Defines the tracking number value.
    /// </summary>
    public const string TrackingNumber = "messaging.vicione-servicebus.tracking_number";

    /// <summary>
    /// Defines the message types value.
    /// </summary>
    public const string MessageTypes = "messaging.vicione-servicebus.message_types";

    /// <summary>
    /// Defines the consumer type value.
    /// </summary>
    public const string ConsumerType = "messaging.vicione-servicebus.consumer_type";

    /// <summary>
    /// Defines the peer address value.
    /// </summary>
    public const string PeerAddress = "peer.address";

    /// <summary>
    /// Defines the begin state value.
    /// </summary>
    public const string BeginState = "messaging.vicione-servicebus.begin_state";
    /// <summary>
    /// Defines the end state value.
    /// </summary>
    public const string EndState = "messaging.vicione-servicebus.end_state";
    /// <summary>
    /// Defines the saga id value.
    /// </summary>
    public const string SagaId = "messaging.vicione-servicebus.saga_id";


    /// <summary>
    /// Provides an exceptions implementation.
    /// </summary>
    public class Exceptions
    {
        /// <summary>
        /// Defines the event name value.
        /// </summary>
        public const string EventName = "exception";
        /// <summary>
        /// Defines the type value.
        /// </summary>
        public const string Type = "exception.type";
        /// <summary>
        /// Defines the message value.
        /// </summary>
        public const string Message = "exception.message";
        /// <summary>
        /// Defines the escaped value.
        /// </summary>
        public const string Escaped = "exception.escaped";
        /// <summary>
        /// Defines the stacktrace value.
        /// </summary>
        public const string Stacktrace = "exception.stacktrace";
    }


    /// <summary>
    /// Provides a messaging implementation.
    /// </summary>
    public static class Messaging
    {
        /// <summary>
        /// Defines the body length value.
        /// </summary>
        public const string BodyLength = "messaging.message.body.size";
        /// <summary>
        /// Defines the conversation id value.
        /// </summary>
        public const string ConversationId = "messaging.message.conversation_id";
        /// <summary>
        /// Defines the destination name value.
        /// </summary>
        public const string DestinationName = "messaging.destination.name";
        /// <summary>
        /// Defines the transport message id value.
        /// </summary>
        public const string TransportMessageId = "messaging.message.id";
        /// <summary>
        /// Defines the operation value.
        /// </summary>
        public const string Operation = "messaging.operation";
        /// <summary>
        /// Defines the system value.
        /// </summary>
        public const string System = "messaging.system";


        /// <summary>
        /// Provides a rabbit mq implementation.
        /// </summary>
        public static class RabbitMq
        {
            /// <summary>
            /// Defines the routing key value.
            /// </summary>
            public const string RoutingKey = "messaging.rabbitmq.destination.routing_key";
        }
    }
}
