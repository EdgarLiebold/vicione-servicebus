// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface RoutingKeySendContext
    {
        /// <summary>
        /// The routing key for the message (defaults to "")
        /// </summary>
        string? RoutingKey { get; set; }
    }
}
