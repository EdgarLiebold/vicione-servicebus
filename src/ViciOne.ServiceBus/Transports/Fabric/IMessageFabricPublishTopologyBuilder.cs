using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Applies publish topology to an in-memory message fabric.</summary>
internal interface IMessageFabricPublishTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>Gets or sets the current message exchange name.</summary>
    string? ExchangeName { get; set; }
    /// <summary>Gets or sets the current message exchange routing behavior.</summary>
    InMemoryExchangeType ExchangeType { get; set; }

    /// <summary>Creates a nested builder for an implemented message type.</summary>
    /// <returns>A builder that forwards declarations to this topology.</returns>
    IMessageFabricPublishTopologyBuilder CreateImplementedBuilder();
}
