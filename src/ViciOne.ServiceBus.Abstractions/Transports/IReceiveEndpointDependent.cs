using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IReceiveEndpointDependent
{
    /// <summary>
    /// The task which is completed once the receive endpoint is completed
    /// </summary>
    Task Completed { get; }
}
