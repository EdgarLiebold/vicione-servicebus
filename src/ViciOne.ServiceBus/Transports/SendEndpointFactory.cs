// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    /// <summary>
    /// Factory method for a send endpoint
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="TKey"></typeparam>
    public delegate Task<ISendEndpoint> SendEndpointFactory<in TKey>(TKey key);
}
