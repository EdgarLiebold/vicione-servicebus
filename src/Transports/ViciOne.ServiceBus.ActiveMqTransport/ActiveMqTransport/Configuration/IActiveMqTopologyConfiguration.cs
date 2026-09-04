using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public interface IActiveMqTopologyConfiguration :
    ITopologyConfiguration
{
    new IActiveMqPublishTopologyConfigurator Publish { get; }

    new IActiveMqSendTopologyConfigurator Send { get; }

    new IActiveMqConsumeTopologyConfigurator Consume { get; }
}
