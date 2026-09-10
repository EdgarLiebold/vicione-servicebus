using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Exposes in-memory endpoint hosting and its logical transport clock.</summary>
internal interface IInMemoryHost :
    IHost<IInMemoryReceiveEndpointConfigurator>
{
    /// <summary>Gets the logical clock shared by the host's message fabric.</summary>
    IInMemoryDelayProvider DelayProvider { get; }
}
