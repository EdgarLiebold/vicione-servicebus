using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for publish pipe.
/// </summary>
public interface IPublishPipe :
    IProbeSite
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
