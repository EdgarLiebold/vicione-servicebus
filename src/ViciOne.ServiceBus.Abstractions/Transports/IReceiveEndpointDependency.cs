using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IReceiveEndpointDependency
{
    /// <summary>
    /// The task which is completed once the receive endpoint is ready
    /// </summary>
    Task Ready { get; }
}
