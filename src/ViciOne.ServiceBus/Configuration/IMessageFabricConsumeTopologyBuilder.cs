namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message fabric consume topology builder.
/// </summary>
public interface IMessageFabricConsumeTopologyBuilder :
    IMessageFabricTopologyBuilder
{
    /// <summary>
    /// Gets or sets the exchange value.
    /// </summary>
    string Exchange { get; set; }
    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    string Queue { get; set; }
}
