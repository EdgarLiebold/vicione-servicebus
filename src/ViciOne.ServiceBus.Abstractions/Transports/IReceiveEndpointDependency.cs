using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for receive endpoint dependency.
/// </summary>
public interface IReceiveEndpointDependency
{
    /// <summary>
    /// The task which is completed once the receive endpoint is ready
    /// </summary>
    Task Ready { get; }
}
