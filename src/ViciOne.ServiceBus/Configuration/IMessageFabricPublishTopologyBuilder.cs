using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds message fabric publish topology components.</summary>
public interface IMessageFabricPublishTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>Gets or sets the exchange name.</summary>
    string ExchangeName { get; set; }
    /// <summary>Gets or sets the exchange type.</summary>
    ExchangeType ExchangeType { get; set; }

    /// <summary>Creates implemented builder.</summary>
    /// <returns>The created implemented builder.</returns>
    IMessageFabricPublishTopologyBuilder CreateImplementedBuilder();
}
