// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts
{
    using System;


    public interface RequestTimeoutExpired<out TRequest>
        where TRequest : class
    {
        /// <summary>
        /// The correlationId of the state machine
        /// </summary>
        Guid CorrelationId { get; }

        /// <summary>
        /// When the request expired
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The expiration time that was scheduled for the request
        /// </summary>
        DateTime ExpirationTime { get; }

        /// <summary>
        /// The requestId of the request
        /// </summary>
        Guid RequestId { get; }

        /// <summary>
        /// The  original request message.
        /// </summary>
        TRequest? Message { get; }
    }
}
