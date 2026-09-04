using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for session id send topology convention.
/// </summary>
public interface ISessionIdSendTopologyConvention :
    ISendTopologyConvention
{
    /// <summary>
    /// The default, non-message specific routing key formatter used by messages
    /// when no specific convention has been specified.
    /// </summary>
    ISessionIdFormatter DefaultFormatter { get; set; }
}
