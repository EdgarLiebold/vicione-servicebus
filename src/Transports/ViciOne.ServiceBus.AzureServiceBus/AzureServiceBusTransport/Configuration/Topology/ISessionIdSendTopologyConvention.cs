using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Provides the default session-id formatter used to create per-message send conventions.</summary>
public interface ISessionIdSendTopologyConvention :
    ISendTopologyConvention
{
    /// <summary>
    /// Gets or sets the default session-id formatter used when no message-specific formatter has been specified.
    /// </summary>
    ISessionIdFormatter DefaultFormatter { get; set; }
}
