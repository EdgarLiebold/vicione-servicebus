// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Serialization;


    public static class MessageHeaders
    {
        /// <summary>
        /// The one place the active wire prefix is written. Every ViciOne header constant is built from it,
        /// so the wire identity has a single owner and cannot drift into a second literal.
        /// </summary>
        public const string Prefix = "VSB-";

        /// <summary>
        /// The group a transport adapter filters on when it moves fault detail across a boundary.
        /// </summary>
        public const string FaultPrefix = Prefix + "Fault-";

        /// <summary>
        /// Set on an outbox message to make its first delivery attempt fail, so a retry can be observed.
        /// </summary>
        public const string FailDelivery = Prefix + "Fail-Delivery";

        /// <summary>
        /// The reason for a message action being taken
        /// </summary>
        public const string Reason = Prefix + "Reason";

        /// <summary>
        /// The type of exception from a Fault
        /// </summary>
        public const string FaultExceptionType = Prefix + "Fault-ExceptionType";

        /// <summary>
        /// The input address of the endpoint on which the fault occurred
        /// </summary>
        public const string FaultInputAddress = Prefix + "Fault-InputAddress";

        /// <summary>
        /// The exception message from a Fault
        /// </summary>
        public const string FaultMessage = Prefix + "Fault-Message";

        /// <summary>
        /// The message type from a Fault
        /// </summary>
        public const string FaultMessageType = Prefix + "Fault-MessageType";

        /// <summary>
        /// The consumer type which faulted
        /// </summary>
        public const string FaultConsumerType = Prefix + "Fault-ConsumerType";

        /// <summary>
        /// The timestamp when the fault occurred
        /// </summary>
        public const string FaultTimestamp = Prefix + "Fault-Timestamp";

        /// <summary>
        /// The stack trace from a Fault
        /// </summary>
        public const string FaultStackTrace = Prefix + "Fault-StackTrace";

        /// <summary>
        /// The number of times the message was retried
        /// </summary>
        public const string FaultRetryCount = Prefix + "Fault-RetryCount";

        /// <summary>
        /// The number of times the message was redelivered
        /// </summary>
        public const string FaultRedeliveryCount = Prefix + "Fault-RedeliveryCount";

        /// <summary>
        /// The endpoint that forwarded the message to the new destination
        /// </summary>
        public const string ForwarderAddress = Prefix + "Forwarder-Address";

        /// <summary>
        /// The tokenId for the message that was registered with the scheduler
        /// </summary>
        public const string SchedulingTokenId = Prefix + "Scheduling-TokenId";

        /// <summary>
        /// The number of times the message has been redelivered (zero if never)
        /// </summary>
        public const string RedeliveryCount = Prefix + "Redelivery-Count";

        /// <summary>
        /// The trigger key that was used when the scheduled message was trigger
        /// </summary>
        public const string QuartzTriggerKey = Prefix + "Quartz-TriggerKey";

        /// <summary>
        /// Identifies the client from which the request is being sent
        /// </summary>
        public const string ClientId = Prefix + "Request-ClientId";

        /// <summary>
        /// Identifies the endpoint that handled the request
        /// </summary>
        public const string EndpointId = Prefix + "Request-EndpointId";

        /// <summary>
        /// The initiating conversation id if a new conversation was started by this message
        /// </summary>
        public const string InitiatingConversationId = Prefix + "InitiatingConversationId";

        /// <summary>
        /// MessageId - <see cref="MessageEnvelope" />
        /// </summary>
        public const string MessageId = "MessageId";

        /// <summary>
        /// CorrelationId - <see cref="MessageEnvelope" />
        /// </summary>
        public const string CorrelationId = "CorrelationId";

        /// <summary>
        /// ConversationId - <see cref="MessageEnvelope" />
        /// </summary>
        public const string ConversationId = "ConversationId";

        /// <summary>
        /// RequestId - <see cref="MessageEnvelope" />
        /// </summary>
        public const string RequestId = "RequestId";

        /// <summary>
        /// InitiatorId - <see cref="MessageEnvelope" />
        /// </summary>
        public const string InitiatorId = Prefix + "InitiatorId";

        /// <summary>
        /// SourceAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string SourceAddress = Prefix + "Source-Address";

        /// <summary>
        /// ResponseAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string ResponseAddress = Prefix + "Response-Address";

        /// <summary>
        /// FaultAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string FaultAddress = Prefix + "Fault-Address";

        /// <summary>
        /// MessageType - <see cref="MessageEnvelope" />
        /// </summary>
        public const string MessageType = Prefix + "MessageType";

        /// <summary>
        /// The Transport message ID, which is a string, because we can't assume anything
        /// </summary>
        public const string TransportMessageId = "TransportMessageId";

        /// <summary>
        /// The Transport sent time (not supported by all, but hopefully enough)
        /// </summary>
        public const string TransportSentTime = "TransportSentTime";

        /// <summary>
        /// When the message is redelivered or scheduled, and a new MessageId was generated, the original messageId
        /// </summary>
        public const string OriginalMessageId = Prefix + "OriginalMessageId";

        /// <summary>
        /// When a transport header is used, this is the name
        /// </summary>
        public const string ContentType = "Content-Type";

        /// <summary>
        /// Used in routing slip variables to store the correlationId of a future
        /// </summary>
        public const string FutureId = "FutureId";


        public static class Host
        {
            /// <summary>
            /// The group a transport adapter filters on when it moves host detail across a boundary.
            /// </summary>
            public const string Prefix = MessageHeaders.Prefix + "Host-";

            public const string Info = MessageHeaders.Prefix + "Host-Info";
            public const string MachineName = MessageHeaders.Prefix + "Host-MachineName";
            public const string ProcessName = MessageHeaders.Prefix + "Host-ProcessName";
            public const string ProcessId = MessageHeaders.Prefix + "Host-ProcessId";
            public const string Assembly = MessageHeaders.Prefix + "Host-Assembly";
            public const string AssemblyVersion = MessageHeaders.Prefix + "Host-AssemblyVersion";
            public const string ViciOneServiceBusVersion = MessageHeaders.Prefix + "Host-ViciOneServiceBusVersion";
            public const string FrameworkVersion = MessageHeaders.Prefix + "Host-FrameworkVersion";
            public const string OperatingSystemVersion = MessageHeaders.Prefix + "Host-OperatingSystemVersion";
        }


        public static class Request
        {
            public const string Accept = MessageHeaders.Prefix + "Request-AcceptType";

            /// <summary>
            /// Tracks routing slip retries when using the RoutingSlipRequestProxy
            /// </summary>
            public const string RoutingSlipRetryCount = MessageHeaders.Prefix + "RoutingSlip-RetryCount";
        }


        public static class Quartz
        {
            /// <summary>
            /// The time when the message was scheduled
            /// </summary>
            public const string Scheduled = MessageHeaders.Prefix + "Quartz-Scheduled";

            /// <summary>
            /// When the event for this message was fired by Quartz
            /// </summary>
            public const string Sent = MessageHeaders.Prefix + "Quartz-Sent";

            /// <summary>
            /// When the next message is scheduled to be sent
            /// </summary>
            public const string NextScheduled = MessageHeaders.Prefix + "Quartz-NextScheduled";

            /// <summary>
            /// When the previous message was sent
            /// </summary>
            public const string PreviousSent = MessageHeaders.Prefix + "Quartz-PreviousSent";

            /// <summary>
            /// Schedule identifier
            /// </summary>
            public const string ScheduleId = MessageHeaders.Prefix + "Quartz-ScheduleId";

            /// <summary>
            /// Schedule group
            /// </summary>
            public const string ScheduleGroup = MessageHeaders.Prefix + "Quartz-ScheduleGroup";
        }
    }
}
