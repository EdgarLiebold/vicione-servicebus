using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Receives messages dispatched by an in-memory queue.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IMessageReceiver<in TMessage> :
    IProbeSite
    where TMessage : class
{
    /// <summary>Delivers a message to the receiver.</summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="cancellationToken">The token that cancels delivery.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeliverAsync(TMessage message, CancellationToken cancellationToken);
}
