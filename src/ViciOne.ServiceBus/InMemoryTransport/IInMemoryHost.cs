using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Defines the operations required by in memory host.</summary>
public interface IInMemoryHost :
    IHost<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Gets the delay provider.</summary>
    IInMemoryDelayProvider DelayProvider { get; }
}
