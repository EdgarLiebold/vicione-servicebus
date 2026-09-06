namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds message fabric consume topology components.</summary>
public interface IMessageFabricConsumeTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>Gets or sets the exchange.</summary>
    string Exchange { get; set; }
    /// <summary>Gets or sets the queue.</summary>
    string Queue { get; set; }
}
