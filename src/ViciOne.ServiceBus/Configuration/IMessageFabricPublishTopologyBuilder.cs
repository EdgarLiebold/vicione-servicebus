using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message fabric publish topology builder.
/// </summary>
public interface IMessageFabricPublishTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>
    /// Gets or sets the exchange name value.
    /// </summary>
    string ExchangeName { get; set; }
    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    ExchangeType ExchangeType { get; set; }

    /// <summary>
    /// Creates implemented builder.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IMessageFabricPublishTopologyBuilder CreateImplementedBuilder();
}
