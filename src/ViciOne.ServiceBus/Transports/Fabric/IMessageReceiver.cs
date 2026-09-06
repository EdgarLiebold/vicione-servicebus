using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Receives messages from a queue.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageReceiver<in T> :
    IProbeSite
    where T : class
{
    /// <summary>Delivers the current message.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeliverAsync(T message, CancellationToken cancellationToken);
}
