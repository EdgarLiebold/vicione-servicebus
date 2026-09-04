using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

public interface IMessageFabricPublishTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    string ExchangeName { get; set; }
    ExchangeType ExchangeType { get; set; }

    IMessageFabricPublishTopologyBuilder CreateImplementedBuilder();
}
