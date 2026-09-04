using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Defines the contract for in memory host.
/// </summary>
public interface IInMemoryHost :
    IHost<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>
    /// Gets the delay provider value.
    /// </summary>
    IInMemoryDelayProvider DelayProvider { get; }
}
