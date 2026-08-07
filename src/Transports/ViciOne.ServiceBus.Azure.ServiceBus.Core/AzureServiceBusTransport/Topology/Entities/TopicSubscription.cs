// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    /// <summary>
    /// A subscription that forwards to another topic
    /// </summary>
    public interface TopicSubscription
    {
        /// <summary>
        /// The source topic
        /// </summary>
        Topic Source { get; }

        /// <summary>
        /// The destination topic
        /// </summary>
        Topic Destination { get; }

        /// <summary>
        /// The subscription that binds them together
        /// </summary>
        Subscription Subscription { get; }
    }
}
