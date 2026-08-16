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
