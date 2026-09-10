using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Accepts messages from an in-memory exchange.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IMessageSink<TMessage> :
    IProbeSite
    where TMessage : class
{
    /// <summary>Delivers a message to this destination.</summary>
    /// <param name="context">The message delivery.</param>
    /// <param name="cancellationToken">The token that cancels this delivery attempt.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeliverAsync(IMessageDeliveryContext<TMessage> context, CancellationToken cancellationToken = default);
}
