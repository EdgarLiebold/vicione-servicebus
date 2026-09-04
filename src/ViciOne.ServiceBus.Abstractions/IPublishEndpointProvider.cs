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
    Task<ISendEndpoint> GetPublishSendEndpoint<T>()
        where T : class;
}
