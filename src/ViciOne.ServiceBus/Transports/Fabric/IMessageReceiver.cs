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
    /// <summary>
    /// Performs the deliver operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeliverAsync(T message, CancellationToken cancellationToken);
}
