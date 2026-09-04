using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public interface IInMemoryTopologyConfiguration :
    ITopologyConfiguration
{
    new IInMemoryPublishTopologyConfigurator Publish { get; }

    new IInMemoryConsumeTopologyConfigurator Consume { get; }
}
