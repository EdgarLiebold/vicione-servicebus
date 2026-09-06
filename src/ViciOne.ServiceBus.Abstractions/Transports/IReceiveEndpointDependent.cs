using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by receive endpoint dependent.</summary>
public interface IReceiveEndpointDependent
{
    /// <summary>The task which is completed once the receive endpoint is completed.</summary>
    Task Completed { get; }
}
