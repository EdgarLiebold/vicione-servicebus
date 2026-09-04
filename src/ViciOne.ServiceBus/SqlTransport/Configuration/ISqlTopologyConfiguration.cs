using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlTopologyConfiguration :
    ITopologyConfiguration
{
    new ISqlPublishTopologyConfigurator Publish { get; }

    new ISqlSendTopologyConfigurator Send { get; }

    new ISqlConsumeTopologyConfigurator Consume { get; }
}
