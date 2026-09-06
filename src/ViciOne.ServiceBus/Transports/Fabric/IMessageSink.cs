using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message sink.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageSink<T> :
    IProbeSite
    where T : class
{
    /// <summary>Delivers the current message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default);
}
