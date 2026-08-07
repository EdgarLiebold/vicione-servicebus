// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Serialization;


    public static class MessageHeaders
    {
        /// <summary>
        /// The reason for a message action being taken
        /// </summary>
        public const string Reason = "ViciOne-ServiceBus-Reason";

        /// <summary>
        /// The type of exception from a Fault
        /// </summary>
        public const string FaultExceptionType = "ViciOne-ServiceBus-Fault-ExceptionType";

        /// <summary>
        /// The input address of the endpoint on which the fault occurred
        /// </summary>
        public const string FaultInputAddress = "ViciOne-ServiceBus-Fault-InputAddress";

        /// <summary>
        /// The exception message from a Fault
        /// </summary>
        public const string FaultMessage = "ViciOne-ServiceBus-Fault-Message";

        /// <summary>
        /// The message type from a Fault
        /// </summary>
        public const string FaultMessageType = "ViciOne-ServiceBus-Fault-MessageType";

        /// <summary>
        /// The consumer type which faulted
        /// </summary>
        public const string FaultConsumerType = "ViciOne-ServiceBus-Fault-ConsumerType";

        /// <summary>
        /// The timestamp when the fault occurred
        /// </summary>
        public const string FaultTimestamp = "ViciOne-ServiceBus-Fault-Timestamp";

        /// <summary>
        /// The stack trace from a Fault
        /// </summary>
        public const string FaultStackTrace = "ViciOne-ServiceBus-Fault-StackTrace";

        /// <summary>
        /// The number of times the message was retried
        /// </summary>
        public const string FaultRetryCount = "ViciOne-ServiceBus-Fault-RetryCount";

        /// <summary>
        /// The number of times the message was redelivered
        /// </summary>
        public const string FaultRedeliveryCount = "ViciOne-ServiceBus-Fault-RedeliveryCount";

        /// <summary>
        /// The endpoint that forwarded the message to the new destination
        /// </summary>
        public const string ForwarderAddress = "ViciOne-ServiceBus-Forwarder-Address";

        /// <summary>
        /// The tokenId for the message that was registered with the scheduler
        /// </summary>
        public const string SchedulingTokenId = "ViciOne-ServiceBus-Scheduling-TokenId";

        /// <summary>
        /// The number of times the message has been redelivered (zero if never)
        /// </summary>
        public const string RedeliveryCount = "ViciOne-ServiceBus-Redelivery-Count";

        /// <summary>
        /// The trigger key that was used when the scheduled message was trigger
        /// </summary>
        public const string QuartzTriggerKey = "ViciOne-ServiceBus-Quartz-TriggerKey";

        /// <summary>
        /// Identifies the client from which the request is being sent
        /// </summary>
        public const string ClientId = "ViciOne-ServiceBus-Request-ClientId";

        /// <summary>
        /// Identifies the endpoint that handled the request
        /// </summary>
        public const string EndpointId = "ViciOne-ServiceBus-Request-EndpointId";

        /// <summary>
        /// The initiating conversation id if a new conversation was started by this message
        /// </summary>
        public const string InitiatingConversationId = "ViciOne-ServiceBus-InitiatingConversationId";

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
        public const string InitiatorId = "ViciOne-ServiceBus-InitiatorId";

        /// <summary>
        /// SourceAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string SourceAddress = "ViciOne-ServiceBus-Source-Address";

        /// <summary>
        /// ResponseAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string ResponseAddress = "ViciOne-ServiceBus-Response-Address";

        /// <summary>
        /// FaultAddress - <see cref="MessageEnvelope" />
        /// </summary>
        public const string FaultAddress = "ViciOne-ServiceBus-Fault-Address";

        /// <summary>
        /// MessageType - <see cref="MessageEnvelope" />
        /// </summary>
        public const string MessageType = "ViciOne-ServiceBus-MessageType";

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
        public const string OriginalMessageId = "ViciOne-ServiceBus-OriginalMessageId";

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
            public const string Info = "ViciOne-ServiceBus-Host-Info";
            public const string MachineName = "ViciOne-ServiceBus-Host-MachineName";
            public const string ProcessName = "ViciOne-ServiceBus-Host-ProcessName";
            public const string ProcessId = "ViciOne-ServiceBus-Host-ProcessId";
            public const string Assembly = "ViciOne-ServiceBus-Host-Assembly";
            public const string AssemblyVersion = "ViciOne-ServiceBus-Host-AssemblyVersion";
            public const string ViciOneServiceBusVersion = "ViciOne-ServiceBus-Host-ViciOneServiceBusVersion";
            public const string FrameworkVersion = "ViciOne-ServiceBus-Host-FrameworkVersion";
            public const string OperatingSystemVersion = "ViciOne-ServiceBus-Host-OperatingSystemVersion";
        }


        public static class Request
        {
            public const string Accept = "ViciOne-ServiceBus-Request-AcceptType";

            /// <summary>
            /// Tracks routing slip retries when using the RoutingSlipRequestProxy
            /// </summary>
            public const string RoutingSlipRetryCount = "ViciOne-ServiceBus-RoutingSlip-RetryCount";
        }


        public static class Quartz
        {
            /// <summary>
            /// The time when the message was scheduled
            /// </summary>
            public const string Scheduled = "ViciOne-ServiceBus-Quartz-Scheduled";

            /// <summary>
            /// When the event for this message was fired by Quartz
            /// </summary>
            public const string Sent = "ViciOne-ServiceBus-Quartz-Sent";

            /// <summary>
            /// When the next message is scheduled to be sent
            /// </summary>
            public const string NextScheduled = "ViciOne-ServiceBus-Quartz-NextScheduled";

            /// <summary>
            /// When the previous message was sent
            /// </summary>
            public const string PreviousSent = "ViciOne-ServiceBus-Quartz-PreviousSent";

            /// <summary>
            /// Schedule identifier
            /// </summary>
            public const string ScheduleId = "ViciOne-ServiceBus-Quartz-ScheduleId";

            /// <summary>
            /// Schedule group
            /// </summary>
            public const string ScheduleGroup = "ViciOne-ServiceBus-Quartz-ScheduleGroup";
        }
    }
}
