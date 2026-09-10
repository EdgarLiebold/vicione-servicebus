namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Applies receive-endpoint topology to an in-memory message fabric.</summary>
internal interface IMessageFabricConsumeTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>Gets the receive endpoint's exchange name.</summary>
    string Exchange { get; }
    /// <summary>Gets the receive endpoint's queue name.</summary>
    string Queue { get; }
}
