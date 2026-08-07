// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IRabbitMqClusterConfigurator
    {
        /// <summary>
        /// Add a node to the cluster, which may include a host name, and an option port number.
        /// </summary>
        /// <param name="nodeAddress">The node address</param>
        void Node(string nodeAddress);
    }
}
