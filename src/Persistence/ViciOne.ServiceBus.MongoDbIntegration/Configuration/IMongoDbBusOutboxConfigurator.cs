// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus
{
    using System;


    public interface IMongoDbBusOutboxConfigurator :
        IBusOutboxConfigurator
    {
        /// <summary>
        /// The number of messages to deliver at a time from the outbox to the broker
        /// </summary>
        public int MessageDeliveryLimit { set; }

        /// <summary>
        /// Transport Send timeout when delivering messages to the transport
        /// </summary>
        TimeSpan MessageDeliveryTimeout { get; set; }
    }
}
