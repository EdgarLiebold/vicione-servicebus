using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines sql topology configuration.</summary>
public interface ISqlTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets the publish.</summary>
    new ISqlPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets the send.</summary>
    new ISqlSendTopologyConfigurator Send { get; }

    /// <summary>Gets the consume.</summary>
    new ISqlConsumeTopologyConfigurator Consume { get; }
}
