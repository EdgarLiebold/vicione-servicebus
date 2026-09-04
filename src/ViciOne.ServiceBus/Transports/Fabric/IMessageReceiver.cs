using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Receives messages from a queue
/// </summary>
public interface IMessageReceiver<in T> :
    IProbeSite
    where T : class
{
    Task DeliverAsync(T message, CancellationToken cancellationToken);
}
