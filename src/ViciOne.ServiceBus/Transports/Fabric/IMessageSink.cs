using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message sink.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageSink<T> :
    IProbeSite
    where T : class
{
    /// <summary>
    /// Performs the deliver operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default);
}
