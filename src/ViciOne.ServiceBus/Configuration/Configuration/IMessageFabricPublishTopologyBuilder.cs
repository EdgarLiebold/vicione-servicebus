// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Transports.Fabric;


    public interface IMessageFabricPublishTopologyBuilder :
        IMessageFabricTopologyBuilder
    {
        string ExchangeName { get; set; }
        ExchangeType ExchangeType { get; set; }

        IMessageFabricPublishTopologyBuilder CreateImplementedBuilder();
    }
}
