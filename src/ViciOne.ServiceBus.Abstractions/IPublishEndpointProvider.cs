// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


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
}
