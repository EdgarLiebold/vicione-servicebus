using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides publish endpoint services.</summary>
public interface IPublishEndpointProvider :
    IPublishObserverConnector
{
    /// <summary>Return the SendEndpoint used for publishing the specified message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class;
}
