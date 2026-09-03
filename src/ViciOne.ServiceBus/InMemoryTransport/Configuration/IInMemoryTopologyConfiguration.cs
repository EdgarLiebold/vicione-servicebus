namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryTopologyConfiguration :
        ITopologyConfiguration
    {
        new IInMemoryPublishTopologyConfigurator Publish { get; }

        new IInMemoryConsumeTopologyConfigurator Consume { get; }
    }
}
