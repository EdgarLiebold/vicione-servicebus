using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IPublishEndpointProvider :
    IPublishObserverConnector
{
    /// <summary>
    /// Return the SendEndpoint used for publishing the specified message
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class;
}
